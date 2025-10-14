using System.Drawing.Drawing2D;

namespace StarResonanceDpsAnalysis.Plugin.Charts
{
    /// <summary>
    /// ��ƽ������ͼ�ؼ�
    /// </summary>
    public class FlatBarChart : UserControl
    {
        #region �ֶκ�����

        private readonly List<BarChartData> _data = new();
        private bool _isDarkTheme = false;
        private string _titleText = "";
        private string _xAxisLabel = "";
        private string _yAxisLabel = "";
        private bool _showLegend = true;

        // �߾����� - ���ٱ߾�������ͼ��ռ��
        private const int PaddingLeft = 35;   // ��60���ٵ�35
        private const int PaddingRight = 15;  // ��20���ٵ�15
        private const int PaddingTop = 25;    // ��10���ӵ�25�����ṩ����ռ�������Ϸ����ı���ǩ
        private const int PaddingBottom = 50; // ��100���ٵ�50

        // �ִ�����ɫ
        private readonly Color[] _colors = {
            Color.FromArgb(74, 144, 226),   // ��
            Color.FromArgb(126, 211, 33),   // ��
            Color.FromArgb(245, 166, 35),   // ��
            Color.FromArgb(208, 2, 27),     // ��
            Color.FromArgb(144, 19, 254),   // ��
            Color.FromArgb(80, 227, 194),   // ��
            Color.FromArgb(184, 233, 134),  // ǳ��
            Color.FromArgb(75, 213, 238),   // ����
            Color.FromArgb(248, 231, 28),   // ��
            Color.FromArgb(189, 16, 224)    // Ʒ��
        };

        public bool IsDarkTheme
        {
            get => _isDarkTheme;
            set
            {
                _isDarkTheme = value;
                ApplyTheme();
                Invalidate();
            }
        }

        public string TitleText
        {
            get => _titleText;
            set
            {
                _titleText = value;
                Invalidate();
            }
        }

        public string XAxisLabel
        {
            get => _xAxisLabel;
            set
            {
                _xAxisLabel = value;
                Invalidate();
            }
        }

        public string YAxisLabel
        {
            get => _yAxisLabel;
            set
            {
                _yAxisLabel = value;
                Invalidate();
            }
        }

        public bool ShowLegend
        {
            get => _showLegend;
            set
            {
                _showLegend = value;
                Invalidate();
            }
        }

        #endregion

        #region ���캯��

        public FlatBarChart()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.DoubleBuffer | ControlStyles.ResizeRedraw, true);

            ApplyTheme();
        }

        #endregion

        #region ���ݹ���

        public void SetData(List<(string Label, double Value)> data)
        {
            _data.Clear();

            for (int i = 0; i < data.Count; i++)
            {
                _data.Add(new BarChartData
                {
                    Label = data[i].Label,
                    Value = data[i].Value,
                    Color = _colors[i % _colors.Length]
                });
            }

            Invalidate();
        }

        public void ClearData()
        {
            _data.Clear();
            Invalidate();
        }

        #endregion

        #region ��������

        private void ApplyTheme()
        {
            if (_isDarkTheme)
            {
                BackColor = Color.FromArgb(31, 31, 31);
                ForeColor = Color.White;
            }
            else
            {
                BackColor = Color.White;
                ForeColor = Color.Black;
            }
        }

        #endregion

        #region ���Ʒ���

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // �������
            g.Clear(BackColor);

            if (_data.Count == 0)
            {
                DrawNoDataMessage(g);
                return;
            }

            // �������ֵ
            var maxValue = _data.Max(d => d.Value);
            if (maxValue <= 0) return;

            // ����ͼ������
            var chartRect = new Rectangle(PaddingLeft, PaddingTop,
                                        Width - PaddingLeft - PaddingRight,
                                        Height - PaddingTop - PaddingBottom);

            // ��������
            DrawGrid(g, chartRect, maxValue);

            // ����������
            DrawAxes(g, chartRect, maxValue);

            // ������״��
            DrawBars(g, chartRect, maxValue);

            // ���Ʊ��⣨����У�
            DrawTitle(g);
        }

        private void DrawNoDataMessage(Graphics g)
        {
            var message = "��������";
            var font = new Font("Microsoft YaHei", 12, FontStyle.Regular);
            var brush = new SolidBrush(_isDarkTheme ? Color.Gray : Color.DarkGray);

            var size = g.MeasureString(message, font);
            var x = (Width - size.Width) / 2;
            var y = (Height - size.Height) / 2;

            g.DrawString(message, font, brush, x, y);

            font.Dispose();
            brush.Dispose();
        }

        private void DrawGrid(Graphics g, Rectangle chartRect, double maxValue)
        {
            var gridColor = _isDarkTheme ? Color.FromArgb(64, 64, 64) : Color.FromArgb(230, 230, 230);
            using var gridPen = new Pen(gridColor, 1);

            // ����ˮƽ������ - ��������������
            for (int i = 0; i <= 5; i++) // ��10�����ٵ�5��
            {
                var y = chartRect.Y + (float)chartRect.Height * i / 5;
                g.DrawLine(gridPen, chartRect.X, y, chartRect.Right, y);
            }
        }

        private void DrawAxes(Graphics g, Rectangle chartRect, double maxValue)
        {
            var axisColor = _isDarkTheme ? Color.FromArgb(128, 128, 128) : Color.FromArgb(180, 180, 180);
            using var axisPen = new Pen(axisColor, 1);
            using var textBrush = new SolidBrush(ForeColor);
            using var font = new Font("Microsoft YaHei", 7); // ��9���ٵ�7

            // ����X��
            g.DrawLine(axisPen, chartRect.X, chartRect.Bottom, chartRect.Right, chartRect.Bottom);

            // ����Y��
            g.DrawLine(axisPen, chartRect.X, chartRect.Y, chartRect.X, chartRect.Bottom);

            // X���ǩ�����ͱ�ǩ��
            var barWidth = (float)chartRect.Width / _data.Count;
            for (int i = 0; i < _data.Count; i++)
            {
                var x = chartRect.X + barWidth * (i + 0.5f);
                var text = _data[i].Label;

                var size = g.MeasureString(text, font);

                // �򻯱�ǩ��ʾ��ֱ��ˮƽ��ʾ������ת
                var textX = x - size.Width / 2;
                var textY = chartRect.Bottom + 5; // ���ټ��

                g.DrawString(text, font, textBrush, textX, textY);
            }

            // Y���ǩ - ����ʾ
            for (int i = 0; i <= 5; i++) // ��10���̶ȼ��ٵ�5��
            {
                var y = chartRect.Bottom - (float)chartRect.Height * i / 5;
                var value = maxValue * i / 5;
                var text = $"{value:F0}%"; // ֱ����ʾ�ٷֱȣ��򻯸�ʽ

                var size = g.MeasureString(text, font);
                g.DrawString(text, font, textBrush, chartRect.X - size.Width - 3, y - size.Height / 2);
            }

            // ���ǩ������У�- ʹ�ø�С����
            if (!string.IsNullOrEmpty(_xAxisLabel))
            {
                var size = g.MeasureString(_xAxisLabel, font);
                var x = chartRect.X + (chartRect.Width - size.Width) / 2;
                var y = chartRect.Bottom + 35; // ����λ��
                g.DrawString(_xAxisLabel, font, textBrush, x, y);
            }

            if (!string.IsNullOrEmpty(_yAxisLabel))
            {
                var size = g.MeasureString(_yAxisLabel, font);
                using var matrix = new Matrix();
                matrix.RotateAt(-90, new PointF(10, chartRect.Y + (chartRect.Height + size.Width) / 2));
                g.Transform = matrix;
                g.DrawString(_yAxisLabel, font, textBrush, 10, chartRect.Y + (chartRect.Height + size.Width) / 2);
                g.ResetTransform();
            }
        }

        private void DrawBars(Graphics g, Rectangle chartRect, double maxValue)
        {
            var barWidth = (float)chartRect.Width / _data.Count * 0.85f; // �������ο�ȴ�0.8f��0.85f
            var barSpacing = (float)chartRect.Width / _data.Count * 0.075f; // ���ټ��

            for (int i = 0; i < _data.Count; i++)
            {
                var data = _data[i];
                var barHeight = (float)(data.Value / maxValue * chartRect.Height);

                var x = chartRect.X + i * (barWidth + barSpacing * 2) + barSpacing;
                var y = chartRect.Bottom - barHeight;

                var barRect = new RectangleF(x, y, barWidth, barHeight);

                // �������� - ��ƽ���ޱ߿����
                using var brush = new SolidBrush(data.Color);
                g.FillRectangle(brush, barRect);

                // ������ֵ��ǩ - ���ܵ�����ǩλ��
                if (barHeight > 15) // ֻ���㹻�ߵ����β���ʾ��ǩ
                {
                    var valueText = $"{data.Value:F1}%"; // ����ֵ��ʽ��ʾ
                    using var font = new Font("Microsoft YaHei", 6, FontStyle.Regular); // ��8���ٵ�6
                    using var textBrush = new SolidBrush(ForeColor);

                    var textSize = g.MeasureString(valueText, font);
                    var textX = x + (barWidth - textSize.Width) / 2;

                    // ����ѡ���ǩλ�ã����ȷ��������Ϸ���������������ڲ�
                    var textAboveY = y - textSize.Height - 2; // �����Ϸ�λ��
                    var textInsideY = y + 2; // �����ڲ��϶�λ��

                    // ����Ϸ�λ���Ƿ����㹻�ռ�
                    var textY = (textAboveY >= chartRect.Y) ? textAboveY : textInsideY;

                    // ȷ����ǩ��ͼ��������
                    if (textY + textSize.Height <= chartRect.Bottom && textY >= chartRect.Y)
                    {
                        // ���ݱ�ǩλ��ѡ����ʵ��ı���ɫ
                        Color textColor = ForeColor;
                        if (textY == textInsideY) // �����ǩ�������ڲ�
                        {
                            // ʹ���뱳���Աȵ���ɫ
                            textColor = GetContrastColor(data.Color);
                        }

                        using var contrastBrush = new SolidBrush(textColor);
                        g.DrawString(valueText, font, contrastBrush, textX, textY);
                    }
                }
            }
        }

        /// <summary>
        /// ���ݱ���ɫ��ȡ�Ա�ɫ����ɫ���ɫ��
        /// </summary>
        private Color GetContrastColor(Color backgroundColor)
        {
            // ����RGB����ֵ
            var brightness = (backgroundColor.R * 0.299 + backgroundColor.G * 0.587 + backgroundColor.B * 0.114);

            // ��������ѡ���ɫ���ɫ��Ϊ�Ա�ɫ
            return brightness > 128 ? Color.Black : Color.White;
        }

        private void DrawTitle(Graphics g)
        {
            if (string.IsNullOrEmpty(_titleText)) return;

            using var font = new Font("Microsoft YaHei", 14, FontStyle.Bold);
            using var brush = new SolidBrush(ForeColor);

            var size = g.MeasureString(_titleText, font);
            var x = (Width - size.Width) / 2;
            var y = 10;

            g.DrawString(_titleText, font, brush, x, y);
        }

        #endregion
    }

    /// <summary>
    /// ����ͼ������
    /// </summary>
    public class BarChartData
    {
        public string Label { get; set; } = "";
        public double Value { get; set; }
        public Color Color { get; set; }
    }
}