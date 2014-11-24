using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;

namespace TrailingSpaceFlagger
{
    public class TrailingSpaceFlagger
    {
        private readonly IAdornmentLayer _layer;
        private readonly IWpfTextView _view;
        private readonly Brush _brush;
        private readonly Pen _pen;
        private static readonly object _adornmentTag = new object();

        public TrailingSpaceFlagger(IWpfTextView view)
        {
            _view = view;
            _layer = view.GetAdornmentLayer("TrailingSpaceFlagger");

            //Listen to any event that changes the layout (text changes, scrolling, etc)
            _view.LayoutChanged += OnLayoutChanged;
            _view.Caret.PositionChanged += OnCaretPositionChanged;

            //Create the pen and brush to color the box behind the a's
            _brush = new SolidColorBrush(Colors.Red);
            _brush.Freeze();
            _pen = new Pen(_brush, 0.5);
            _pen.Freeze();
        }

        /// <summary>
        /// On layout change add the adornment to any reformatted lines
        /// </summary>
        private void OnLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
        {
            foreach (var line in e.NewOrReformattedLines)
            {
                CreateVisuals(line);
            }
        }


        private void OnCaretPositionChanged(object sender, CaretPositionChangedEventArgs eventArgs)
        {
            CreateVisuals(_view.GetTextViewLineContainingBufferPosition(eventArgs.OldPosition.BufferPosition));
        }

        /// <summary>
        /// Within the given line add the scarlet box behind trailing spaces
        /// </summary>
        private void CreateVisuals(ITextViewLine line)
        {
            //grab a reference to the lines in the current TextView
            var textViewLines = _view.TextViewLines;
            int start = line.Start;
            int end = line.End;

            var isCurrentLine = _view.Caret.ContainingTextViewLine == line;

            for (var i = end - 1; i >= start; i--)
            {
                if (_view.TextSnapshot[i] == ' ' || _view.TextSnapshot[i] == '\t')
                {
                    var span = new SnapshotSpan(_view.TextSnapshot, Span.FromBounds(i, i + 1));
                    if (!isCurrentLine || i >= _view.Caret.Position.BufferPosition.Position)
                    {
                        var g = textViewLines.GetMarkerGeometry(span);
                        if (g != null)
                        {
                            var drawing = new GeometryDrawing(_brush, _pen, g);
                            drawing.Freeze();

                            var drawingImage = new DrawingImage(drawing);
                            drawingImage.Freeze();

                            var image = new Image {Source = drawingImage};

                            //Align the image with the top of the bounds of the text geometry
                            Canvas.SetLeft(image, g.Bounds.Left);
                            Canvas.SetTop(image, g.Bounds.Top);

                            _layer.AddAdornment(AdornmentPositioningBehavior.TextRelative, span, _adornmentTag, image, null);
                        }
                    }
                    else
                    {
                        _layer.RemoveMatchingAdornments(span, a => a.Tag == _adornmentTag);
                    }
                }
                else
                {
                    break;
                }
            }
        }
    }
}
