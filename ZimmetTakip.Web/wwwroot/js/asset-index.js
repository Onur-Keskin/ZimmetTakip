// Asset/Index sayfasının JavaScript kodu (jQuery)
// 1) Canlı arama: yazdıkça AJAX ile arama yapıp tablo gövdesini yeniden çizer
// 2) Detay satırı: ana satıra tıklayınca altındaki detay satırı slideToggle ile açılır/kapanır
// 3) Arızalı (faulty) varlık satırları kırmızı arka planla vurgulanır

// Sayfa (DOM) tamamen yüklendikten sonra çalış
$(function () {
    var $searchInput = $('#assetSearch');
    var $tableBody = $('#assetTableBody');

    // Arama ve satır çizimi için gereken adresler (Razor view'da data- özniteliklerine yazıldı)
    var searchUrl = $searchInput.data('search-url');
    var editUrl = $tableBody.data('edit-url');
    var deleteUrl = $tableBody.data('delete-url');

    // Debounce için zamanlayıcı ve devam eden son AJAX isteği
    var searchTimer = null;
    var currentRequest = null;

    // ---------------------------------------------------------------
    // Arızalı satırları vurgula: data-status="faulty" olan satırlara Bootstrap table-danger sınıfı eklenir.
    // Hem sayfa ilk yüklendiğinde hem de arama sonrası tablo yeniden çizildiğinde çağrılır.
    // ---------------------------------------------------------------
    function highlightFaultyRows() {
        $tableBody.find('tr.asset-row[data-status="faulty"]').addClass('table-danger');
    }

    // ---------------------------------------------------------------
    // Tabloya tek hücreli bilgi satırı ekler (örn. "Kayıt bulunamadı")
    // ---------------------------------------------------------------
    function showMessageRow(message) {
        var $cell = $('<td>')
            .attr('colspan', 7)
            .addClass('text-center text-muted py-4')
            .text(message);
        $tableBody.append($('<tr>').append($cell));
    }

    // ---------------------------------------------------------------
    // Detay satırındaki bir bilgi çiftini (başlık + değer) oluşturur
    // ---------------------------------------------------------------
    function addDetailItem($list, title, value) {
        $list.append($('<dt>').addClass('col-sm-2').text(title));
        $list.append($('<dd>').addClass('col-sm-10').text(value || '-'));
    }

    // ---------------------------------------------------------------
    // Bir varlık için ana satır + detay satırı oluşturur (sunucudaki Razor yapısının aynısı).
    // Metinler .text() ile eklenir; böylece veritabanındaki içerik HTML olarak yorumlanmaz (XSS koruması).
    // ---------------------------------------------------------------
    function buildAssetRows(asset) {
        // Ana satır
        var $row = $('<tr>')
            .addClass('asset-row')
            .attr('data-status', asset.status);

        var brandAndModel = ((asset.brand || '') + ' ' + (asset.model || '')).trim();
        var $badge = $('<span>')
            .addClass('badge ' + asset.statusBadgeClass)
            .text(asset.statusText);

        var $editLink = $('<a>')
            .attr('href', editUrl + '/' + asset.assetId)
            .addClass('btn btn-sm btn-outline-primary')
            .text('Düzenle');
        var $deleteLink = $('<a>')
            .attr('href', deleteUrl + '/' + asset.assetId)
            .addClass('btn btn-sm btn-outline-danger')
            .text('Sil');

        $row.append($('<td>').addClass('fw-semibold').text(asset.assetName));
        $row.append($('<td>').text(asset.categoryName));
        $row.append($('<td>').text(brandAndModel));
        $row.append($('<td>').append($('<code>').text(asset.serialNo)));
        $row.append($('<td>').append($badge));
        $row.append($('<td>').text(asset.purchaseDate || ''));
        $row.append($('<td>').addClass('text-end text-nowrap').append($editLink, ' ', $deleteLink));

        // Detay satırı (başlangıçta gizli)
        var $detailList = $('<dl>').addClass('row mb-0 small');
        addDetailItem($detailList, 'Kategori', asset.categoryName);
        addDetailItem($detailList, 'Alım Tarihi', asset.purchaseDate);
        addDetailItem($detailList, 'Açıklama', asset.description);

        var $detailContent = $('<div>')
            .addClass('asset-detail-content bg-light px-4 py-3')
            .hide()
            .append($detailList);

        var $detailRow = $('<tr>')
            .addClass('asset-detail-row')
            .hide()
            .append($('<td>').attr('colspan', 7).addClass('p-0').append($detailContent));

        return [$row, $detailRow];
    }

    // ---------------------------------------------------------------
    // AJAX ile arama yap ve dönen JSON ile tablo gövdesini yeniden çiz
    // ---------------------------------------------------------------
    function searchAssets(term) {
        // Önceki istek hâlâ sürüyorsa iptal et (eski sonuç yenisinin üzerine yazmasın)
        if (currentRequest !== null) {
            currentRequest.abort();
        }

        currentRequest = $.ajax({
            url: searchUrl,
            type: 'GET',
            data: { term: term },
            dataType: 'json'
        })
            .done(function (assets) {
                // Tabloyu temizle ve gelen her varlık için satırları ekle
                $tableBody.empty();

                if (assets.length === 0) {
                    showMessageRow('Kayıt bulunamadı');
                    return;
                }

                $.each(assets, function (index, asset) {
                    var rows = buildAssetRows(asset);
                    $tableBody.append(rows[0], rows[1]);
                });

                // Yeni çizilen satırlarda da arızalı olanları vurgula
                highlightFaultyRows();
            })
            .fail(function (xhr, textStatus) {
                // Kendi iptal ettiğimiz istek hata sayılmaz
                if (textStatus === 'abort') {
                    return;
                }
                $tableBody.empty();
                showMessageRow('Arama sırasında bir hata oluştu. Lütfen tekrar deneyin.');
            })
            .always(function () {
                currentRequest = null;
            });
    }

    // ---------------------------------------------------------------
    // Canlı arama: her tuş bırakıldığında 300 ms beklenir (debounce).
    // Kullanıcı yazmaya devam ederse önceki bekleme iptal edilir; böylece her harf için istek gitmez.
    // ---------------------------------------------------------------
    $searchInput.on('keyup', function () {
        var term = $(this).val();

        clearTimeout(searchTimer);
        searchTimer = setTimeout(function () {
            searchAssets(term);
        }, 300);
    });

    // Arama kutusundaki "x" ile temizlenince de (keyup olmadan) listeyi yenile
    $searchInput.on('search', function () {
        clearTimeout(searchTimer);
        searchAssets($(this).val());
    });

    // ---------------------------------------------------------------
    // Detay satırı: ana satıra tıklanınca hemen altındaki detay satırı açılır/kapanır.
    // Olay tbody'ye bağlanır (event delegation); böylece arama sonrası eklenen satırlarda da çalışır.
    // ---------------------------------------------------------------
    $tableBody.on('click', 'tr.asset-row', function (event) {
        // Düzenle / Sil butonlarına tıklanınca detay açılmasın, link normal çalışsın
        if ($(event.target).closest('a, button').length > 0) {
            return;
        }

        var $detailRow = $(this).next('tr.asset-detail-row');
        var $content = $detailRow.find('.asset-detail-content');

        // Tablo satırı (tr) kayarak açılamadığı için içindeki div'e slideToggle uygulanır;
        // satır açılırken önce gösterilir, kapanma animasyonu bitince tekrar gizlenir.
        $detailRow.show();
        $content.slideToggle(200, function () {
            if (!$content.is(':visible')) {
                $detailRow.hide();
            }
        });
    });

    // Sayfa ilk açıldığında arızalı satırları vurgula
    highlightFaultyRows();
});
