using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MediaBrowser.UI.Controls
{
    /// <summary>
    /// Follow steps 1a or 1b and then 2 to use this custom control in a XAML file.
    ///
    /// Step 1a) Using this custom control in a XAML file that exists in the current project.
    /// Add this XmlNamespace attribute to the root element of the markup file where it is 
    /// to be used:
    ///
    ///     xmlns:MyNamespace="clr-namespace:MediaBrowser.UI.Controls"
    ///
    ///
    /// Step 1b) Using this custom control in a XAML file that exists in a different project.
    /// Add this XmlNamespace attribute to the root element of the markup file where it is 
    /// to be used:
    ///
    ///     xmlns:MyNamespace="clr-namespace:MediaBrowser.UI.Controls;assembly=MediaBrowser.UI.Controls"
    ///
    /// You will also need to add a project reference from the project where the XAML file lives
    /// to this project and Rebuild to avoid compilation errors:
    ///
    ///     Right click on the target project in the Solution Explorer and
    ///     "Add Reference"->"Projects"->[Browse to and select this project]
    ///
    ///
    /// Step 2)
    /// Go ahead and use your control in the XAML file.
    ///
    ///     <MyNamespace:ExtendedImage/>
    ///
    /// </summary>
    public class ExtendedImage : Control
    {
        public static readonly DependencyProperty HasImageProperty = DependencyProperty.Register(
            "HasImage", 
            typeof (bool), 
            typeof (ExtendedImage),
            new PropertyMetadata(default(bool)));

        public bool HasImage
        {
            get { return (bool)GetValue(HasImageProperty); }
            set { SetValue(HasImageProperty, value); }
        }

        public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
            "Source", 
            typeof(ImageSource), 
            typeof(ExtendedImage), 
            new PropertyMetadata(default(ImageBrush)));

        public ImageSource Source
        {
            get { return (ImageSource)GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }

        public static readonly DependencyProperty StretchProperty = DependencyProperty.Register(
            "Stretch", 
            typeof (Stretch), 
            typeof (ExtendedImage), 
            new PropertyMetadata(default(Stretch)));

        public Stretch Stretch
        {
            get { return (Stretch) GetValue(StretchProperty); }
            set { SetValue(StretchProperty, value); }
        }

        public static readonly DependencyProperty PlaceHolderSourceProperty = DependencyProperty.Register(
            "PlaceHolderSource",
            typeof(ImageSource),
            typeof(ExtendedImage),
            new PropertyMetadata(default(ImageBrush)));

        public ImageSource PlaceHolderSource
        {
            get { return (ImageSource)GetValue(PlaceHolderSourceProperty); }
            set { SetValue(PlaceHolderSourceProperty, value); }
        }

        static ExtendedImage()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ExtendedImage),
                new FrameworkPropertyMetadata(typeof(ExtendedImage)));
        }
    }
}
