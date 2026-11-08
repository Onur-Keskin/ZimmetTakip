// Assignment/Index (Zimmetler) sayfasının JavaScript kodu (jQuery)
// AJAX iade: "İade Al" butonu modal açar, onaylanınca iade $.ajax POST ile yapılır.
// Başarıda satır aktif sekmeden kaybolur (fadeOut), geçmiş sekmesine eklenir ve toast mesajı gösterilir.
// JS kapalıysa "İade Al" linki normal Return sayfasına gider (bu dosya hiç çalışmaz).

// Sayfa (DOM) tamamen yüklendikten sonra çalış
$(function () {
    var $activeTableBody = $('#activeTableBody');
    var $historyTableBody = $('#historyTableBody');
    var $returnForm = $('#returnForm');
    var $returnSubmit = $('#returnSubmit');

    // Bootstrap modal ve toast nesneleri
    var returnModal = new bootstrap.Modal(document.getElementById('returnModal'));
    var toastElement = document.getElementById('resultToast');

    // ---------------------------------------------------------------
    // Sağ altta Bootstrap toast ile mesaj gösterir (başarı: yeşil, hata: kırmızı)
    // ---------------------------------------------------------------
    function showToast(message, isSuccess) {
        $(toastElement)
            .removeClass('text-bg-success text-bg-danger')
            .addClass(isSuccess ? 'text-bg-success' : 'text-bg-danger');
        $('#resultToastBody').text(message);

        bootstrap.Toast.getOrCreateInstance(toastElement).show();
    }

    // ---------------------------------------------------------------
    // Sekme başlıklarındaki sayıları tablolardaki satır sayısına göre günceller
    // ---------------------------------------------------------------
    function updateCounts() {
        $('#activeCount').text($activeTableBody.find('tr.active-row').length);
        $('#historyCount').text($historyTableBody.find('tr.history-row').length);
    }

    // ---------------------------------------------------------------
    // Tabloya "kayıt yok" bilgi satırı ekler
    // ---------------------------------------------------------------
    function addEmptyRow($tableBody, columnCount, message) {
        var $cell = $('<td>')
            .attr('colspan', columnCount)
            .addClass('text-center text-muted py-4')
            .text(message);
        $tableBody.append($('<tr>').addClass('empty-row').append($cell));
    }

    // ---------------------------------------------------------------
    // Sunucudan dönen bilgilerle geçmiş sekmesine yeni satır ekler (en üste).
    // Metinler .text() ile eklenir; veritabanındaki içerik HTML olarak yorumlanmaz (XSS koruması).
    // ---------------------------------------------------------------
    function addHistoryRow(result) {
        // "Zimmet geçmişi bulunmuyor" satırı varsa kaldır
        $historyTableBody.find('tr.empty-row').remove();

        var $row = $('<tr>').addClass('history-row');
        $row.append($('<td>').addClass('fw-semibold').text(result.assetName));
        $row.append($('<td>').append($('<code>').text(result.serialNo)));
        $row.append($('<td>').text(result.userFullName));
        $row.append($('<td>').text(result.assignedAt));
        $row.append($('<td>').text(result.returnedAt));
        $row.append($('<td>').text(result.assignmentNote || ''));
        $row.append($('<td>').text(result.returnNote || ''));

        $historyTableBody.prepend($row);
    }

    // ---------------------------------------------------------------
    // Aktif sekmedeki satırı fadeOut ile kaldırır; tablo boşalırsa bilgi satırı ekler
    // ---------------------------------------------------------------
    function removeActiveRow(assignmentId) {
        var $row = $activeTableBody.find('tr.active-row[data-assignment-id="' + assignmentId + '"]');

        $row.fadeOut(400, function () {
            $(this).remove();

            if ($activeTableBody.find('tr.active-row').length === 0) {
                addEmptyRow($activeTableBody, 6, 'Aktif zimmet bulunmuyor.');
            }
            updateCounts();
        });
    }

    // ---------------------------------------------------------------
    // "İade Al" butonu: Return sayfasına gitmek yerine modal penceresini açar.
    // Olay tbody'ye bağlanır (event delegation).
    // ---------------------------------------------------------------
    $activeTableBody.on('click', '.js-return-button', function (event) {
        event.preventDefault();

        var $button = $(this);

        // Modal içindeki alanları seçilen zimmetin bilgileriyle doldur
        $('#returnAssignmentId').val($button.data('assignment-id'));
        $('#returnAssetName').text($button.data('asset-name'));
        $('#returnUserName').text($button.data('user-name'));
        $('#returnNote').val('');

        returnModal.show();
    });

    // ---------------------------------------------------------------
    // Modal formu gönderilince: sayfa yenilenmeden $.ajax ile POST yap.
    // serialize() formdaki tüm alanları (id, iade notu ve __RequestVerificationToken) birlikte gönderir.
    // ---------------------------------------------------------------
    $returnForm.on('submit', function (event) {
        event.preventDefault();

        // Çift tıklamayla iki kez gönderilmesin diye butonu pasif yap
        $returnSubmit.prop('disabled', true);

        $.ajax({
            url: $returnForm.attr('action'),
            type: 'POST',
            data: $returnForm.serialize(),
            dataType: 'json'
        })
            .done(function (result) {
                // Sunucu iş kuralı hatası döndürdüyse (örn. zaten iade edilmiş) mesajı göster
                if (!result.success) {
                    showToast(result.message, false);
                    return;
                }

                // Başarılı: modalı kapat, satırı geçmişe taşı, sayıları güncelle, mesaj göster
                returnModal.hide();
                removeActiveRow(result.assignmentId);
                addHistoryRow(result);
                updateCounts();
                showToast(result.message, true);
            })
            .fail(function () {
                // Ağ hatası, sunucu hatası veya geçersiz anti-forgery token
                showToast('İade sırasında bir hata oluştu. Lütfen sayfayı yenileyip tekrar deneyin.', false);
            })
            .always(function () {
                $returnSubmit.prop('disabled', false);
            });
    });
});
