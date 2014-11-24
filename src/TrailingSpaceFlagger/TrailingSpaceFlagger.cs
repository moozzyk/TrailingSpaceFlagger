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
            var currentLine = _view.Caret.ContainingTextViewLine;

            foreach (var line in e.NewOrReformattedLines.Except(new[] { currentLine }))
            {
                CreateVisuals(line);
            }
        }

        /// <summary>
        /// Within the given line add the scarlet box behind the a
        /// </summary>

        private void OnCaretPositionChanged(object sender, CaretPositionChangedEventArgs eventArgs)
        {
            if (_view.GetTextViewLineContainingBufferPosition(eventArgs.OldPosition.BufferPosition) !=
                _view.GetTextViewLineContainingBufferPosition(eventArgs.NewPosition.BufferPosition))
            {
                CreateVisuals(_view.GetTextViewLineContainingBufferPosition(eventArgs.OldPosition.BufferPosition));
            }
        }

        private void CreateVisuals(ITextViewLine line)
        {
            //grab a reference to the lines in the current TextView 
            var textViewLines = _view.TextViewLines;
            int start = line.Start;
            int end = line.End;

            //Loop through each character, and place a box around any a 
            for (var i = end - 1; (i >= start); --i)
            {
                if (_view.TextSnapshot[i] == ' ' || _view.TextSnapshot[i] == '\t')
                {
                    var span = new SnapshotSpan(_view.TextSnapshot, Span.FromBounds(i, i + 1));
                    var g = textViewLines.GetMarkerGeometry(span);
                    if (g != null)
                    {
                        var drawing = new GeometryDrawing(_brush, _pen, g);
                        drawing.Freeze();

                        var drawingImage = new DrawingImage(drawing);
                        drawingImage.Freeze();

                        var image = new Image { Source = drawingImage };

                        //Align the image with the top of the bounds of the text geometry
                        Canvas.SetLeft(image, g.Bounds.Left);
                        Canvas.SetTop(image, g.Bounds.Top);

                        _layer.AddAdornment(AdornmentPositioningBehavior.TextRelative, span, null, image, null);
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
