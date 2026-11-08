// Assignment/Create sayfasının JavaScript kodu (jQuery)
// Cascade dropdown: Kategori seçilince o kategorideki "Depoda" varlıklar AJAX ile Varlık listesine doldurulur.
// Kategori seçilmeden Varlık listesi pasif (disabled) kalır.
// Not: Sunucu tarafındaki kontroller (varlık depoda mı, aktif zimmeti var mı vb.) aynen geçerlidir.

// Sayfa (DOM) tamamen yüklendikten sonra çalış
$(function () {
    var $category = $('#CategoryId');
    var $asset = $('#AssetId');

    // Kategoriye göre varlıkları getiren JSON adresi (view'da data-url özniteliğine yazıldı)
    var assetsUrl = $asset.data('url');

    // ---------------------------------------------------------------
    // Varlık listesini sadece ilk seçenekle (açıklama metni) bırakır
    // ---------------------------------------------------------------
    function resetAssetList(placeholderText) {
        $asset.empty();
        $asset.append($('<option>').val('').text(placeholderText));
    }

    // ---------------------------------------------------------------
    // Seçilen kategorinin depodaki varlıklarını AJAX ile yükler.
    // selectedAssetId: form hatayla geri döndüyse önceden seçilmiş varlığı tekrar seçmek için
    // ---------------------------------------------------------------
    function loadAssets(categoryId, selectedAssetId) {
        resetAssetList('-- Varlık seçiniz --');

        // Kategori seçilmediyse varlık listesi pasif kalır
        if (!categoryId) {
            $asset.prop('disabled', true);
            return;
        }

        // Yükleme bitene kadar listeyi pasif yap
        $asset.prop('disabled', true);
        resetAssetList('Yükleniyor...');

        $.ajax({
            url: assetsUrl,
            type: 'GET',
            data: { categoryId: categoryId },
            dataType: 'json'
        })
            .done(function (assets) {
                // Bu kategoride depoda varlık yoksa bilgi ver, liste pasif kalsın
                if (assets.length === 0) {
                    resetAssetList('-- Bu kategoride depoda varlık yok --');
                    return;
                }

                // Gelen her varlık için bir <option> ekle (.text() ile güvenli şekilde)
                resetAssetList('-- Varlık seçiniz --');
                $.each(assets, function (index, item) {
                    $asset.append($('<option>').val(item.value).text(item.text));
                });

                // Daha önce seçilmiş varlık bu listede varsa tekrar seç
                if (selectedAssetId) {
                    $asset.val(String(selectedAssetId));
                }

                $asset.prop('disabled', false);
            })
            .fail(function () {
                resetAssetList('-- Varlıklar yüklenemedi, tekrar deneyin --');
            });
    }

    // Kategori değişince o kategorinin varlıklarını yükle
    $category.on('change', function () {
        loadAssets($(this).val(), null);
    });

    // Sayfa ilk açıldığında: kategori seçili değilse varlık listesi pasif olur;
    // form hatayla geri döndüyse (kategori seçili) varlıklar yüklenip önceki seçim korunur.
    loadAssets($category.val(), $asset.val());
});
