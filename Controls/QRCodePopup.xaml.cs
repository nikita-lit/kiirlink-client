using QRCoder;

namespace KiirLink.Controls;

public partial class QRCodePopup
{
    public ImageSource ImageSource { get; }
    
    public QRCodePopup(string url)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);

        var png = new PngByteQRCode(data);
        var bytes = png.GetGraphic(20);

        ImageSource = ImageSource.FromStream(() => new MemoryStream(bytes));
        
        InitializeComponent();
        BindingContext = this;
    }
}