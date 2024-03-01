using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ImageMagick;
using Microsoft.Win32;

namespace image_compress;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private ViewModel vm => (ViewModel)DataContext;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = new ViewModel();
        ResetTitle();
        OptimizedPreviewBox.Visibility = Visibility.Collapsed;
    }

    private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog();
        var result = dialog.ShowDialog();

        if (!result.Value) return;

        var filePath = dialog.FileName;
        vm.SetSelectedImageFileInfo(new FileInfo(filePath));
        OptimizedPreviewBox.Visibility = Visibility.Collapsed;
    }

    private void OptimizeBtn_OnClick(object sender, RoutedEventArgs e)
    {
        if (vm.SelectedImage == null) return;
        vm.IsOptimizing = true;

        LoaderElm.Visibility = Visibility.Visible;
        this.Title = "Optimizing image";

        OptimizedPreviewBox.Visibility = Visibility.Visible;
        var tempPath = Directory.CreateTempSubdirectory();
        var filePath = vm.SelectedImageFileInfo.FullName;
        var tempFileName = Path.Combine(tempPath.FullName, Guid.NewGuid() + "." + Path.GetExtension(filePath));

        File.Copy(filePath, tempFileName);

        var imageInfo = new FileInfo(tempFileName);

        var optimizer = new ImageOptimizer();
        if (vm.SelectedMode == "Lossless")
        {
            optimizer.LosslessCompress(imageInfo);
        }
        else
        {
            optimizer.OptimalCompression = true;
            optimizer.Compress(imageInfo);
        }

        imageInfo.Refresh();
        vm.SetOptimizedImageFileInfo(imageInfo);
        vm.IsOptimizing = false;
        LoaderElm.Visibility = Visibility.Collapsed;
        ResetTitle();
    }

    private void ResetTitle()
    {
        this.Title = "Optimize images";
    }

    private void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        vm.SelectedMode = ModeSelectorElm.SelectedValue as string;
    }
}

public class CompressionMode
{
    public string Value { get; set; }
    public string Label { get; set; }
}

public class ViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;

    private ImageSource selectedImage;
    private ImageSource optimizedImage;

    public FileInfo SelectedImageFileInfo { get; protected set; }
    public FileInfo OptimizedImageFileInfo { get; protected set; }

    public void SetSelectedImageFileInfo(FileInfo info)
    {
        SelectedImageFileInfo = info;
        SelectedImage = new BitmapImage(new Uri(info.FullName));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(selectedImage)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedImageInfoStr)));
        SetOptimizedImageFileInfo(null);
    }

    public void SetOptimizedImageFileInfo(FileInfo info)
    {
        OptimizedImageFileInfo = info;
        OptimizedImage = info == null ? null : new BitmapImage(new Uri(info.FullName));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(OptimizedImage)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(OptimizedImageInfoStr)));
    }

    public CompressionMode[] Modes = new[]
    {
        new CompressionMode() { Label = "Lossless", Value = "Lossless" },
        new CompressionMode() { Label = "Lossy (May result in degraded image)", Value = "Lossy" },
    };

    public CompressionMode[] GetModes
    {
        get { return Modes; }
    }

    private string _selectedMode = "Lossless";
    private bool _isOptimizing;

    public string SelectedMode
    {
        get => _selectedMode;
        set
        {
            _selectedMode = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedMode)));
        }
    }

    public ImageSource SelectedImage { get; protected set; }

    public string SelectedImageInfoStr
    {
        get
        {
            if (SelectedImageFileInfo == null) return "";

            return $"Size: {SizeHumanizer.Humanize((SelectedImageFileInfo!.Length))}";
        }
    }

    public string OptimizedImageInfoStr
    {
        get
        {
            if (OptimizedImageFileInfo == null) return "";

            var str = $"Size: {SizeHumanizer.Humanize((OptimizedImageFileInfo!.Length))}";
            var diff = Math.Abs(SelectedImageFileInfo.Length - OptimizedImageFileInfo.Length);
            var diffStr = SizeHumanizer.Humanize(diff);
            if (OptimizedImageFileInfo.Length < SelectedImageFileInfo.Length)
            {
                str += $" ( -{diffStr})";
            }
            else
            {
                str += $" ({diffStr})";
            }

            return str;
        }
    }

    public ImageSource OptimizedImage { get; protected set; }

    public bool IsOptimizing
    {
        get => _isOptimizing;
        set
        {
            _isOptimizing = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsOptimizing)));
        }
    }
}

public static class SizeHumanizer
{
    public static string Humanize(long length) // in bytes
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        const int unit = 1024;

        if (length == 0)
        {
            return "0 B";
        }

        int magnitude = (int)Math.Log(length, unit);
        double adjustedSize = length / Math.Pow(unit, magnitude);

        return $"{adjustedSize:##.##} {sizes[magnitude]}";
    }
}