using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace StarResonanceDpsAnalysis.Control
{
    /// <summary>
    /// ͸������ť�ؼ�
    /// </summary>
    public partial class TransparencyKnob : UserControl
    {
        private int _value = 100;
        private int _minimum = 10;
        private int _maximum = 100;
        private bool _isDragging = false;
        private Point _lastMousePosition;
        private double _lastValidAngle = 45; // ��¼�ϴ���Ч�Ƕȣ���ʼ��Ϊ���ֵ��Ӧ�Ƕ�
        private Color _knobColor = Color.FromArgb(34, 151, 244);
        private Color _trackColor = Color.FromArgb(220, 220, 220);
        private Color _textColor = Color.FromArgb(160, 160, 160);
        private Color _highlightColor = Color.FromArgb(100, 34, 151, 244);
        private Color _indicatorColor = Color.FromArgb(180, 50, 50);
        private Color _outerIndicatorColor = Color.FromArgb(255, 50, 150, 255);
        private Color _startMarkerColor = Color.FromArgb(180, 180, 180);
        private Color _endMarkerColor = Color.FromArgb(120, 120, 120);
        private Color _centerColor = Color.FromArgb(200, 200, 200); // ���Ĵ�ɫ
        private Color _centerBorderColor = Color.FromArgb(160, 160, 160); // ���ı߿���ɫ
        private float _textSize = 6.3f;
        private int _textOffsetY = 15;
        private bool _isDarkMode = false;
        private bool _isHovering = false;

        public event EventHandler<int> ValueChanged;

        /// <summary>
        /// ָʾ����ɫ
        /// </summary>
        [Browsable(true)]
        [Category("���")]
        [Description("��ťָʾ�����ɫ")]
        [DefaultValue(typeof(Color), "180, 50, 50")]
        public Color IndicatorColor
        {
            get => _indicatorColor;
            set
            {
                if (_indicatorColor != value)
                {
                    _indicatorColor = value;
                    Invalidate();
                }
            }
        }

        /// <summary>
        /// ��Ȧָʾ������ɫ
        /// </summary>
        [Browsable(true)]
        [Category("���")]
        [Description("��Ȧλ��ָʾ��������ɫ")]
        [DefaultValue(typeof(Color), "50, 150, 255")]
        public Color OuterIndicatorColor
        {
            get => _outerIndicatorColor;
            set
            {
                if (_outerIndicatorColor != value)
                {
                    _outerIndicatorColor = value;
                    Invalidate();
                }
            }
        }

        /// <summary>
        /// ��ʼ���ʶ��ɫ
        /// </summary>
        [Browsable(true)]
        [Category("���")]
        [Description("��ʼ���ʶ����ɫ")]
        [DefaultValue(typeof(Color), "180, 180, 180")]
        public Color StartMarkerColor
        {
            get => _startMarkerColor;
            set
            {
                if (_startMarkerColor != value)
                {
                    _startMarkerColor = value;
                    Invalidate();
                }
            }
        }

        /// <summary>
        /// �������ʶ��ɫ
        /// </summary>
        [Browsable(true)]
        [Category("���")]
        [Description("�������ʶ����ɫ")]
        [DefaultValue(typeof(Color), "120, 120, 120")]
        public Color EndMarkerColor
        {
            get => _endMarkerColor;
            set
            {
                if (_endMarkerColor != value)
                {
                    _endMarkerColor = value;
                    Invalidate();
                }
            }
        }

        /// <summary>
        /// ������ɫ
        /// </summary>
        [Browsable(true)]
        [Category("���")]
        [Description("��ť���ĵĴ�ɫ�����ɫ")]
        [DefaultValue(typeof(Color), "200, 200, 200")]
        public Color CenterColor
        {
            get => _centerColor;
            set
            {
                if (_centerColor != value)
                {
                    _centerColor = value;
                    Invalidate();
                }
            }
        }

        /// <summary>
        /// ���ı߿���ɫ
        /// </summary>
        [Browsable(true)]
        [Category("���")]
        [Description("��ť���ı߿����ɫ")]
        [DefaultValue(typeof(Color), "160, 160, 160")]
        public Color CenterBorderColor
        {
            get => _centerBorderColor;
            set
            {
                if (_centerBorderColor != value)
                {
                    _centerBorderColor = value;
                    Invalidate();
                }
            }
        }

        /// <summary>
        /// ������ɫ
        /// </summary>
        [Browsable(true)]
        [Category("���")]
        [Description("��ʾ���ֵ���ɫ")]
        [DefaultValue(typeof(Color), "160, 160, 160")]
        public Color TextColor
        {
            get => _textColor;
            set
            {
                if (_textColor != value)
                {
                    _textColor = value;
                    Invalidate();
                }
            }
        }

        /// <summary>
        /// ���ִ�С
        /// </summary>
        [Browsable(true)]
        [Category("���")]
        [Description("��ʾ���ֵ������С")]
        [DefaultValue(6.3f)]
        public float TextSize
        {
            get => _textSize;
            set
            {
                if (_textSize != value && value > 0)
                {
                    _textSize = value;
                    Invalidate();
                }
            }
        }

        /// <summary>
        /// ���ִ�ֱƫ��
        /// </summary>
        [Browsable(true)]
        [Category("���")]
        [Description("�����������ť�Ĵ�ֱƫ����")]
        [DefaultValue(15)]
        public int TextOffsetY
        {
            get => _textOffsetY;
            set
            {
                if (_textOffsetY != value)
                {
                    _textOffsetY = value;
                    Invalidate();
                }
            }
        }

        public int Value
        {
            get => _value;
            set
            {
                if (value < _minimum) value = _minimum;
                if (value > _maximum) value = _maximum;
                if (_value != value)
                {
                    _value = value;
                    Invalidate();
                    ValueChanged?.Invoke(this, _value);
                }
            }
        }

        public int Minimum
        {
            get => _minimum;
            set
            {
                _minimum = value;
                if (_value < _minimum) Value = _minimum;
            }
        }

        public int Maximum
        {
            get => _maximum;
            set
            {
                _maximum = value;
                if (_value > _maximum) Value = _maximum;
            }
        }

        public bool IsDarkMode
        {
            get => _isDarkMode;
            set
            {
                _isDarkMode = value;
                UpdateColors();
                Invalidate();
            }
        }

        public TransparencyKnob()
        {
            InitializeComponent();
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.DoubleBuffer |
                     ControlStyles.ResizeRedraw, true);

            Size = new Size(160, 140);
            UpdateColors();
        }

        private void UpdateColors()
        {
            if (_isDarkMode)
            {
                _knobColor = Color.FromArgb(34, 151, 244);
                _trackColor = Color.FromArgb(80, 80, 80);
                _highlightColor = Color.FromArgb(150, 34, 151, 244);
                BackColor = Color.Transparent;
            }
            else
            {
                _knobColor = Color.FromArgb(34, 151, 244);
                _trackColor = Color.FromArgb(220, 220, 220);
                _highlightColor = Color.FromArgb(100, 34, 151, 244);
                BackColor = Color.Transparent;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var rect = ClientRectangle;
            var center = new Point(rect.Width / 2, rect.Height / 2 - 10);
            var radius = Math.Min(rect.Width - 40, rect.Height - 40) / 2 - 15;

            // ���㵱ǰֵ��Ӧ�ĽǶ� (��-225�ȵ�45�ȣ��ܹ�270��)
            var angle = -225 + (270.0 * (_value - _minimum) / (_maximum - _minimum));
            var angleRad = angle * Math.PI / 180;

            // ������ʼ�ͽ������ʶ
            DrawStartEndMarkers(g, center, radius);

            // ������Բ����Ӱ
            if (!_isDarkMode)
            {
                var shadowRect = new Rectangle(center.X - radius - 2, center.Y - radius - 2, (radius + 2) * 2, (radius + 2) * 2);
                using (var shadowBrush = new SolidBrush(Color.FromArgb(30, 0, 0, 0)))
                {
                    g.FillEllipse(shadowBrush, shadowRect);
                }
            }

            // ������Բ�� (���) - �����µ����µ�270�Ȼ�
            var trackRect = new Rectangle(center.X - radius, center.Y - radius, radius * 2, radius * 2);
            using (var pen = new Pen(_trackColor, 6))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                g.DrawArc(pen, trackRect, -225, 270);
            }

            // ���������Ȼ� - ʹ�ø����Ե���ɫ
            using (var pen = new Pen(_knobColor, 6))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                if (angle > -225)
                    g.DrawArc(pen, trackRect, -225, (float)(angle + 225));
            }

            // ������Ȧλ��ָʾ���� - �������
            DrawOuterPositionIndicator(g, center, radius, angle);

            // ���Ƽ����������
            DrawCenterDesign(g, center);

            // ������ťָʾ�㣨��������Ե��ʹ���Զ�����ɫ��
            DrawKnobIndicator(g, center, radius, angleRad);

            // �������֣�ʹ���û��Զ��������
            var labelText = "͸����";
            var valueText = _value.ToString();
            var combinedText = $"{labelText} {valueText}";

            using (var font = new Font("HarmonyOS Sans SC", _textSize))
            using (var brush = new SolidBrush(_textColor))
            {
                var textSize = g.MeasureString(combinedText, font);
                var textRect = new PointF(center.X - textSize.Width / 2, center.Y + radius + _textOffsetY);
                g.DrawString(combinedText, font, brush, textRect);
            }

            // ���ƿ̶ȱ��
            DrawScaleMarks(g, center, radius);
        }

        private void DrawCenterDesign(Graphics g, Point center)
        {
            var centerRadius = 25;
            var centerRect = new Rectangle(center.X - centerRadius, center.Y - centerRadius, centerRadius * 2, centerRadius * 2);

            // ����Բ��Ӱ
            if (!_isDarkMode)
            {
                var shadowCenterRect = new Rectangle(center.X - centerRadius - 2, center.Y - centerRadius - 2, (centerRadius + 2) * 2, (centerRadius + 2) * 2);
                using (var shadowBrush = new SolidBrush(Color.FromArgb(30, 0, 0, 0)))
                {
                    g.FillEllipse(shadowBrush, shadowCenterRect);
                }
            }

            // ���ƴ�ɫ�������滻ԭ�еĽ���Ч��
            using (var centerBrush = new SolidBrush(_centerColor))
            {
                g.FillEllipse(centerBrush, centerRect);
            }

            // ����Բ�߿� - ʹ���û��Զ�����ɫ
            using (var pen = new Pen(_centerBorderColor, 2))
            {
                g.DrawEllipse(pen, centerRect);
            }

            // �Ƴ����н���Ч�����߹�Ч��������ͼ����װ�ε� - ���ִ������������
        }

        private void DrawStartEndMarkers(Graphics g, Point center, int radius)
        {
            // �������½���ʼ������½ǽ�����λ��
            var startAngleRad = -225 * Math.PI / 180; // ���½�
            var endAngleRad = 45 * Math.PI / 180;     // ���½�

            var markerRadius = radius + 20;

            // ��ʼ��λ��
            var startX = center.X + markerRadius * Math.Cos(startAngleRad);
            var startY = center.Y + markerRadius * Math.Sin(startAngleRad);

            // ������λ��  
            var endX = center.X + markerRadius * Math.Cos(endAngleRad);
            var endY = center.Y + markerRadius * Math.Sin(endAngleRad);

            // ������ʼ����ţ�С�㣩- ʹ���û��Զ�����ɫ
            using (var brush = new SolidBrush(_startMarkerColor))
            {
                g.FillEllipse(brush, (float)startX - 3, (float)startY - 3, 6, 6);
            }

            // ���ƽ�������ţ��Դ�ĵ㣩- ʹ���û��Զ�����ɫ
            using (var brush = new SolidBrush(_endMarkerColor))
            {
                g.FillEllipse(brush, (float)endX - 4, (float)endY - 4, 8, 8);
            }
        }

        private void DrawKnobIndicator(Graphics g, Point center, int radius, double angleRad)
        {
            // ����ָʾ��λ�ã��� radius + 5 ��Ϊ radius + 6
            var indicatorRadius = radius + 6; // ��΢�����ƶ�һ��
            var indicatorX = center.X + indicatorRadius * Math.Cos(angleRad);
            var indicatorY = center.Y + indicatorRadius * Math.Sin(angleRad);

            // ָʾ����Ӱ
            if (!_isDarkMode)
            {
                using (var shadowBrush = new SolidBrush(Color.FromArgb(80, 0, 0, 0)))
                {
                    var shadowRect = new Rectangle((int)indicatorX - 2, (int)indicatorY - 2, 5, 5);
                    g.FillEllipse(shadowBrush, shadowRect);
                }
            }

            // ֱ�ӻ���ָʾ�����壨ʹ���û��Զ�����ɫ��
            using (var brush = new SolidBrush(_indicatorColor))
            {
                var indicatorRect = new Rectangle((int)indicatorX - 2, (int)indicatorY - 2, 4, 4);
                g.FillEllipse(brush, indicatorRect);
            }

            // ��ָʾ���ڲ����һ��С�ĸ���������ǿ�����
            using (var brush = new SolidBrush(Color.FromArgb(200, 255, 255, 255))) // ��ɫ�߹�
            {
                var highlightRect = new Rectangle((int)indicatorX, (int)indicatorY, 1, 1);
                g.FillEllipse(brush, highlightRect);
            }
        }

        private void DrawScaleMarks(Graphics g, Point center, int radius)
        {
            using (var pen = new Pen(Color.FromArgb(80, _textColor.R, _textColor.G, _textColor.B), 1.5f))
            {
                // ����Ҫλ�û��ƿ̶ȱ��
                for (int i = 0; i <= 4; i++)
                {
                    var angle = -225 + (270.0 * i / 4);
                    var angleRad = angle * Math.PI / 180;
                    var markLength = 10;

                    var outerX = center.X + (radius - 6) * Math.Cos(angleRad);
                    var outerY = center.Y + (radius - 6) * Math.Sin(angleRad);
                    var innerX = center.X + (radius - 6 - markLength) * Math.Cos(angleRad);
                    var innerY = center.Y + (radius - 6 - markLength) * Math.Sin(angleRad);

                    g.DrawLine(pen, (float)innerX, (float)innerY, (float)outerX, (float)outerY);
                }
            }
        }

        private void DrawOuterPositionIndicator(Graphics g, Point center, int radius, double currentAngle)
        {
            // ֻ�е�ǰ�Ƕȴ�����ʼ�Ƕ�ʱ�Ż�����Ȧλ��ָʾ����
            if (currentAngle <= -225) return;

            var outerRadius = radius + 12; // ���еľ���
            var startAngle = -225;
            var sweepAngle = currentAngle - startAngle;

            if (sweepAngle > 0)
            {
                var outerRect = new Rectangle(center.X - outerRadius, center.Y - outerRadius, outerRadius * 2, outerRadius * 2);

                // �򵥵�����Ȧָʾ���� - ������Ҫ̫��
                using (var pen = new Pen(_outerIndicatorColor, 4))
                {
                    pen.StartCap = LineCap.Round;
                    pen.EndCap = LineCap.Round;
                    g.DrawArc(pen, outerRect, (float)startAngle, (float)sweepAngle);
                }
            }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _isHovering = true;
            // �Ƴ�Invalidate()������Ҫ�ػ���ͣЧ��
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _isHovering = false;
            // �Ƴ�Invalidate()������Ҫ�ػ���ͣЧ��
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left)
            {
                _isDragging = true;
                _lastMousePosition = e.Location;

                // ���ݵ�ǰֵ��ʼ��_lastValidAngle
                _lastValidAngle = -225 + (270.0 * (_value - _minimum) / (_maximum - _minimum));

                UpdateValueFromMouse(e.Location);
                Capture = true;
                Invalidate(); // ֻ����קʱ�ػ�
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_isDragging)
            {
                UpdateValueFromMouse(e.Location);
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button == MouseButtons.Left)
            {
                _isDragging = false;
                Capture = false;
                Invalidate(); // ֻ����ק����ʱ�ػ�
            }
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            var delta = e.Delta > 0 ? 5 : -5;
            Value = _value + delta;
        }

        private void UpdateValueFromMouse(Point mousePos)
        {
            var center = new Point(Width / 2, Height / 2 - 10);
            var dx = mousePos.X - center.X;
            var dy = mousePos.Y - center.Y;

            // ����������ʹ������ĵ�
            if (Math.Abs(dx) < 1 && Math.Abs(dy) < 1) return;

            var angle = Math.Atan2(dy, dx) * 180 / Math.PI;

            // ���Ƕ�ת��Ϊ0-360��Χ
            if (angle < 0) angle += 360;

            // ��ȫ������ƽǶ�ӳ���߼� - �޸���Ծ����
            double normalizedAngle;

            // ��ť��Ч��Χ��-225�� �� +45�� (��270��)
            // ��Ӧ���λ�ã����½�(-225��) �� ���½�(45��)

            if (angle >= 0 && angle <= 45)
            {
                // ��������0��-45�� -> ֱ�Ӷ�Ӧ��ť��0��-45��(��ֵ����)
                normalizedAngle = angle;
            }
            else if (angle > 45 && angle < 135)
            {
                // �Ҳ������������ѡ��߽磬��ֹ��Ծ
                if (_isDragging)
                {
                    // ��קʱ��ѡ����ӽ��ϴ�λ�õı߼�
                    var distTo45 = Math.Min(Math.Abs(angle - 45), Math.Abs(angle - (45 + 360)));
                    var distTo225 = Math.Min(Math.Abs(angle + 225), Math.Abs(angle - (225 - 360)));
                    normalizedAngle = distTo45 < distTo225 ? 45 : -225;
                }
                else
                {
                    // ����קʱ�������ֵ
                    normalizedAngle = 45;
                }
            }
            else if (angle >= 135 && angle <= 225)
            {
                // �Ϸ���������򣺶�Ӧ��ť��-225��(��Сֵ)
                normalizedAngle = -225;
            }
            else if (angle > 225 && angle < 315)
            {
                // ���·�����225��-315�� -> ����ӳ�䵽-225�㵽-45��
                // ��������Ӧ����Сֵ���е�ֵ�Ĺ���
                var mappedRange = (angle - 225) / 90.0; // 0.0 to 1.0
                normalizedAngle = -225 + mappedRange * 180; // -225�� to -45��
            }
            else // angle >= 315 && angle < 360
            {
                // �·�����315��-360�� -> ����ӳ�䵽-45�㵽0��
                // ��������Ӧ���е�ֵ����ֵ�Ĺ���
                var mappedRange = (angle - 315) / 45.0; // 0.0 to 1.0  
                normalizedAngle = -45 + mappedRange * 45; // -45�� to 0��
            }

            // ��קʱ����Ծ���ͱ���
            if (_isDragging)
            {
                var angleDiff = Math.Abs(normalizedAngle - _lastValidAngle);
                // ����Ƕȱ仯̫��(����180��)����������Ծ��ʹ�ý�������
                if (angleDiff > 180)
                {
                    var direction = normalizedAngle > _lastValidAngle ? 1 : -1;
                    normalizedAngle = _lastValidAngle + direction * Math.Min(angleDiff, 30);
                }
            }

            // ȷ���Ƕ�����Ч��Χ��
            normalizedAngle = Math.Max(-225, Math.Min(45, normalizedAngle));

            // �����ϴ���Ч�Ƕ�
            _lastValidAngle = normalizedAngle;

            // �����Ӧ����ֵ
            var progress = (normalizedAngle + 225) / 270.0; // 0.0 to 1.0
            var newValue = _minimum + (int)Math.Round(progress * (_maximum - _minimum));

            // ȷ����ֵ����Ч��Χ��
            newValue = Math.Max(_minimum, Math.Min(_maximum, newValue));

            // ��ӵ��������������������
#if DEBUG
            Console.WriteLine($"Mouse: ({mousePos.X}, {mousePos.Y}), Angle: {angle:F1}��, Normalized: {normalizedAngle:F1}��, Progress: {progress:F2}, Value: {newValue}");
#endif

            Value = newValue;
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // TransparencyKnob
            // 
            this.Name = "TransparencyKnob";
            this.Size = new System.Drawing.Size(160, 140);
            this.ResumeLayout(false);
        }
    }
}