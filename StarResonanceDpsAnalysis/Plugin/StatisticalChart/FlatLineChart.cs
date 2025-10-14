using System.Drawing.Drawing2D;

namespace StarResonanceDpsAnalysis.Plugin.Charts
{
    /// <summary>
    /// ��ƽ������ͼ�ؼ� - ֧���϶������ź�ʵʱˢ�¹���
    /// </summary>
    public class FlatLineChart : UserControl
    {
        #region �ֶκ�����

        private readonly List<LineChartSeries> _series = new();
        private bool _isDarkTheme = false;
        private string _titleText = "";
        private string _xAxisLabel = "";
        private string _yAxisLabel = "";
        private bool _showLegend = true;
        private bool _showGrid = true;
        private bool _showViewInfo = false;
        private bool _autoScaleFont = true; // ������������������Ӧ

        // �߾����� - �����߾�ʹͼ������룬���ֽ�խ�Ŀ��
        private const int PaddingLeft = 60;    // �ָ�����������߾�
        private int _paddingRight = 160;       // ��Ϊʵ��������֧�ֶ�̬����
        private const int PaddingTop = 35;     // ���ֶ����߾�
        private const int PaddingBottom = 45;  // ���ֵײ��߾�

        // ����������
        private int _verticalGridLines = 5;    // ��ֱ������������Ĭ��6���ߣ�0-5��

        // �����С���ã�������С�������ͼ���С������
        private const float BaseTitleFontSize = 12f;    // ��С���������14��12
        private const float BaseAxisLabelFontSize = 8f;  // �������ǩ�����9��8
        private const float BaseAxisValueFontSize = 7f;  // ��������ֵ�����8��7
        private const float BaseLegendFontSize = 7f;     // ��Сͼ�������8��7
        private const float BaseNoDataFontSize = 9f;     // ��С��������ʾ�����10��9

        // ���ź���ͼ���
        private float _timeScale = 1.0f;
        private float _viewOffset = 0.0f;
        private float _currentTimeSeconds = 0.0f;

        // ���ݳ־û�
        private readonly Dictionary<string, List<PointF>> _persistentData = new();

        // ��꽻�����
        private Point _lastMousePosition;
        private bool _isPanning = false;
        private ToolTip _tooltip;
        private bool _showTooltip = false;
        private string _tooltipText = "";

        // ʵʱˢ�����
        private System.Windows.Forms.Timer _refreshTimer;
        private bool _autoRefreshEnabled = false;
        private int _refreshInterval = 1000;
        private Action _refreshCallback;

        // ��ͼ�������
        private bool _preserveViewOnDataUpdate = true; // �������������ݸ���ʱ�Ƿ񱣳���ͼ
        private DateTime _lastUserInteraction = DateTime.MinValue; // ��������¼����û�����ʱ��
        private const double UserInteractionCooldownMs = double.MaxValue; // �޸ģ��������ڵ��û���������ʱ��
        private bool _hasUserInteracted = false; // ����������û��Ƿ��й�����

        // ����Ӧ�������
        private float _fontScaleFactor = 1.0f;
        private const float MinFontSize = 6f;
        private const float MaxFontSize = 24f;
        private const int BaseFontSize = 8; // ���������С
        private const int BaseWidth = 400; // �������
        private const int BaseHeight = 200; // �����߶�

        // ��ɫ����
        private readonly Color[] _colors = {
            Color.FromArgb(255, 99, 132),   // ��
            Color.FromArgb(54, 162, 235),   // ��
            Color.FromArgb(255, 206, 86),   // ��
            Color.FromArgb(75, 192, 192),   // ��
            Color.FromArgb(153, 102, 255),  // ��
            Color.FromArgb(255, 159, 64),   // ��
            Color.FromArgb(199, 199, 199),  // ��
            Color.FromArgb(83, 102, 255),   // ����
            Color.FromArgb(255, 99, 255),   // Ʒ��
            Color.FromArgb(99, 255, 132),   // ��
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

        public bool ShowGrid
        {
            get => _showGrid;
            set
            {
                _showGrid = value;
                Invalidate();
            }
        }

        public bool ShowViewInfo
        {
            get => _showViewInfo;
            set
            {
                _showViewInfo = value;
                Invalidate();
            }
        }

        public bool AutoScaleFont
        {
            get => _autoScaleFont;
            set
            {
                _autoScaleFont = value;
                Invalidate();
            }
        }

        public bool AutoRefreshEnabled
        {
            get => _autoRefreshEnabled;
            set
            {
                _autoRefreshEnabled = value;
                if (_refreshTimer != null)
                {
                    _refreshTimer.Enabled = value;
                }
            }
        }

        public int RefreshInterval
        {
            get => _refreshInterval;
            set
            {
                _refreshInterval = Math.Max(100, value);
                if (_refreshTimer != null)
                {
                    _refreshTimer.Interval = _refreshInterval;
                }
            }
        }

        public bool PreserveViewOnDataUpdate
        {
            get => _preserveViewOnDataUpdate;
            set
            {
                _preserveViewOnDataUpdate = value;
            }
        }

        /// <summary>
        /// ��ȡ��ǰʱ������
        /// </summary>
        public float GetTimeScale()
        {
            return _timeScale;
        }

        /// <summary>
        /// ��ȡ��ǰ��ͼƫ��
        /// </summary>
        public float GetViewOffset()
        {
            return _viewOffset;
        }

        /// <summary>
        /// ���ͼ���Ƿ�������
        /// </summary>
        public bool HasData()
        {
            return _series.Count > 0 && _series.Any(s => s.Points.Count > 0);
        }

        /// <summary>
        /// ����û��Ƿ��й�����
        /// </summary>
        public bool HasUserInteracted()
        {
            return _hasUserInteracted;
        }

        /// <summary>
        /// �����Ҳ��ڱ߾�
        /// </summary>
        public void SetPaddingRight(int paddingRight)
        {
            _paddingRight = Math.Max(10, paddingRight); // ��СֵΪ10
            Invalidate();
        }

        /// <summary>
        /// ��ȡ��ǰ�Ҳ��ڱ߾�
        /// </summary>
        public int GetPaddingRight()
        {
            return _paddingRight;
        }

        /// <summary>
        /// ���ô�ֱ����������
        /// </summary>
        public void SetVerticalGridLines(int lineCount)
        {
            _verticalGridLines = Math.Max(3, Math.Min(10, lineCount)); // �޸ģ����Ʒ�Χ3-10֮�䣬��20��Ϊ10
            Invalidate();
        }

        /// <summary>
        /// ��ȡ��ǰ��ֱ����������
        /// </summary>
        public int GetVerticalGridLines()
        {
            return _verticalGridLines;
        }

        #endregion

        #region ���캯��

        public FlatLineChart()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.DoubleBuffer | ControlStyles.ResizeRedraw |
                     ControlStyles.Selectable | ControlStyles.UserMouse, true);

            // ��ʼ��������ʾ
            _tooltip = new ToolTip
            {
                AutoPopDelay = 5000,
                InitialDelay = 100,
                ReshowDelay = 500,
                ShowAlways = true,
                IsBalloon = true
            };

            // ��ʼ��ʵʱˢ�¶�ʱ��
            _refreshTimer = new System.Windows.Forms.Timer
            {
                Interval = _refreshInterval,
                Enabled = false
            };
            _refreshTimer.Tick += RefreshTimer_Tick;

            ApplyTheme();

            // ע������¼�
            MouseMove += OnChartMouseMove;
            MouseWheel += OnChartMouseWheel;
            MouseDown += OnChartMouseDown;
            MouseUp += OnChartMouseUp;
            MouseEnter += OnChartMouseEnter;
            KeyDown += OnChartKeyDown;

            // ����ؼ����ս����Դ�������¼�
            TabStop = true;
        }

        #endregion

        #region ʵʱˢ�·���

        /// <summary>
        /// ����ˢ�»ص�����
        /// </summary>
        public void SetRefreshCallback(Action callback)
        {
            _refreshCallback = callback;
        }

        /// <summary>
        /// ����ʵʱˢ��
        /// </summary>
        public void StartAutoRefresh(int intervalMs = 1000)
        {
            RefreshInterval = intervalMs;
            AutoRefreshEnabled = true;
        }

        /// <summary>
        /// ֹͣʵʱˢ��
        /// </summary>
        public void StopAutoRefresh()
        {
            AutoRefreshEnabled = false;
        }

        private void RefreshTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                // ����û������϶��������˴�ˢ���Ա����жϲ���
                if (_isPanning)
                {
                    return;
                }

                // ���ֹͣץ���ˣ���ȫ�����û�����ͼ״̬�������ص�
                if (!ChartVisualizationService.IsCapturing)
                {
                    // ֹͣץ����ִֻ������ˢ�»ص�������ȫ������ͼ״̬
                    _refreshCallback?.Invoke();
                    Invalidate();
                    return;
                }

                // ���浱ǰ����ͼ״̬ - ��Զ�����û����õ���ͼ
                var currentTimeScale = _timeScale;
                var currentViewOffset = _viewOffset;

                _refreshCallback?.Invoke();

                // �����������ͼ���ֹ��ܣ���Զ�ָ��û������ã��Ƴ�ʱ�����ƣ�
                if (_preserveViewOnDataUpdate)
                {
                    _timeScale = currentTimeScale;
                    _viewOffset = currentViewOffset;
                    ClampViewOffset(); // ����Լ��ƫ������ȷ����Ч��
                }

                Invalidate();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ͼ���Զ�ˢ��ʱ����: {ex.Message}");
            }
        }

        #endregion

        #region ��������Ӧ����

        /// <summary>
        /// ����ͼ���С��������Ӧ�����С
        /// </summary>
        private float CalculateScaledFontSize(float baseFontSize)
        {
            if (!_autoScaleFont) return baseFontSize;

            // ����ͼ���Ⱥ͸߶ȼ�����������
            var baseWidth = 500f;  // ��߻�׼��ȴ�400��500
            var baseHeight = 200f; // ���ֻ�׼�߶�200

            var widthScale = Width / baseWidth;
            var heightScale = Height / baseHeight;

            // ȡ��С���������ӣ������������
            var scale = Math.Min(widthScale, heightScale);

            // �����ص����ŷ�Χ���������ֹ���
            scale = Math.Max(0.7f, Math.Min(1.4f, scale)); // ������Χ��0.6-1.8��0.7-1.4

            return baseFontSize * scale;
        }

        /// <summary>
        /// ���������С���������С���������ǩ����Ҫ����ϸ���Ƶ�����
        /// </summary>
        private float CalculateScaledFontSizeForArea(float baseFontSize, float areaWidth, float areaHeight)
        {
            if (!_autoScaleFont) return baseFontSize;

            // ���ݿ������������ʵ������С
            var baseAreaWidth = 300f;  // ��߻�׼��ȴ�200��300
            var baseAreaHeight = 120f; // ��߻�׼�߶ȴ�100��120

            var widthScale = areaWidth / baseAreaWidth;
            var heightScale = areaHeight / baseAreaHeight;

            var scale = Math.Min(widthScale, heightScale);

            // �����ص����ŷ�Χ���������ֹ�����С
            scale = Math.Max(0.8f, Math.Min(1.2f, scale)); // ������Χ��0.7-1.5��0.8-1.2

            return baseFontSize * scale;
        }

        /// <summary>
        /// ��������Ӧ����
        /// </summary>
        private Font CreateScaledFont(string fontFamily, float baseFontSize, FontStyle style = FontStyle.Regular)
        {
            var scaledSize = CalculateScaledFontSize(baseFontSize);
            // ���ϸ�������С���ƣ��������ֹ���
            scaledSize = Math.Max(6f, Math.Min(16f, scaledSize)); // �����ֵ��24����16
            return new Font(fontFamily, scaledSize, style);
        }

        /// <summary>
        /// ������������Ӧ����
        /// </summary>
        private Font CreateScaledFontForArea(string fontFamily, float baseFontSize, float areaWidth, float areaHeight, FontStyle style = FontStyle.Regular)
        {
            var scaledSize = CalculateScaledFontSizeForArea(baseFontSize, areaWidth, areaHeight);
            scaledSize = Math.Max(6f, Math.Min(14f, scaledSize)); // �����ֵ��20����14
            return new Font(fontFamily, scaledSize, style);
        }

        #endregion

        #region ���ݹ���

        public void AddSeries(string name, List<PointF> points)
        {
            // ���浱ǰ����ͼ״̬
            var currentTimeScale = _timeScale;
            var currentViewOffset = _viewOffset;
            // ���ֹͣץ���ˣ����Ǳ��ֵ�ǰ��ͼ
            var shouldPreserveView = _series.Count > 0 || !ChartVisualizationService.IsCapturing;

            _persistentData[name] = new List<PointF>(points);

            var series = new LineChartSeries
            {
                Name = name,
                Points = new List<PointF>(points),
                Color = _colors[_series.Count % _colors.Length],
                LineWidth = 3.5f
            };

            _series.Add(series);

            if (points.Count > 0)
            {
                _currentTimeSeconds = Math.Max(_currentTimeSeconds, points.Max(p => p.X));
            }

            // ���Ӧ�ñ�����ͼ����ָ�֮ǰ�����ź�ƫ��
            if (shouldPreserveView)
            {
                _timeScale = currentTimeScale;
                _viewOffset = currentViewOffset;
                // ֹͣץ��ʱ��������ͼƫ��
                if (ChartVisualizationService.IsCapturing)
                {
                    ClampViewOffset();
                }
            }

            Invalidate();
        }

        public void ClearSeries()
        {
            _series.Clear();
            // ֻ������ȷ���ʱ��������ͼ������Ҫ����Ƿ����û�����
            if (!_hasUserInteracted)
            {
                ResetViewToDefault();
            }
            Invalidate();
        }

        public void UpdateSeries(string name, List<PointF> points)
        {
            // ���浱ǰ����ͼ״̬
            var currentTimeScale = _timeScale;
            var currentViewOffset = _viewOffset;

            _persistentData[name] = new List<PointF>(points);

            var series = _series.FirstOrDefault(s => s.Name == name);
            if (series != null)
            {
                series.Points = new List<PointF>(points);

                if (points.Count > 0)
                {
                    _currentTimeSeconds = Math.Max(_currentTimeSeconds, points.Max(p => p.X));
                }

                // ���ֹͣץ���ˣ���ȫ�����û�����ͼ״̬
                if (!ChartVisualizationService.IsCapturing)
                {
                    _timeScale = currentTimeScale;
                    _viewOffset = currentViewOffset;
                    // ֹͣץ��ʱ������ClampViewOffset
                }
                else
                {
                    // ����ץ��ʱ���ָ��û�����ͼ״̬�������������ʱ������ͼ
                    _timeScale = currentTimeScale;
                    _viewOffset = currentViewOffset;
                    ClampViewOffset();
                }

                Invalidate();
            }
        }

        public void ReloadPersistentData()
        {
            _series.Clear();
            int colorIndex = 0;

            foreach (var kvp in _persistentData)
            {
                var series = new LineChartSeries
                {
                    Name = kvp.Key,
                    Points = new List<PointF>(kvp.Value),
                    Color = _colors[colorIndex % _colors.Length],
                    LineWidth = 3.5f
                };
                _series.Add(series);
                colorIndex++;
            }

            Invalidate();
        }

        #endregion

        #region ��ͼ����

        public void SetTimeScale(float scale)
        {
            var oldScale = _timeScale;
            _timeScale = Math.Max(0.1f, Math.Min(10.0f, scale));

            // ��ȡ����ǰ�����ͼ���
            var oldViewWidth = GetViewTimeRange(oldScale);
            var newViewWidth = GetViewTimeRange(_timeScale);

            // ���㵱ǰ��ͼ�����ĵ㣨�û����ڲ鿴��λ�ã�
            var currentViewCenter = _viewOffset + oldViewWidth / 2;

            // �Ե�ǰ��ͼ����Ϊ��׼����ƫ�����������û���ǰ�鿴��λ��
            _viewOffset = currentViewCenter - newViewWidth / 2;

            // ֻ����ץ��״̬ʱ��������ͼƫ��
            if (ChartVisualizationService.IsCapturing)
            {
                ClampViewOffset();
            }

            Invalidate();
        }

        public void SetViewOffset(float offset)
        {
            _viewOffset = offset;
            // ֻ����ץ��״̬ʱ��������ͼƫ��
            if (ChartVisualizationService.IsCapturing)
            {
                ClampViewOffset();
            }
            Invalidate();
        }

        public void ResetViewToDefault()
        {
            _timeScale = 1.0f;
            // �޸�Ĭ����ͼƫ�ƣ�ʹ���0�뿪ʼ��ʾ5�뷶Χ
            _viewOffset = Math.Max(0, _currentTimeSeconds - 5);
            ClampViewOffset();
            Invalidate();
        }

        public void ResetZoomAndPan()
        {
            ResetViewToDefault();
        }

        private void ClampViewOffset()
        {
            // ���ֹͣץ���ˣ���ȫ������ͼƫ�����ƣ��û����������϶����κ�λ��
            if (!ChartVisualizationService.IsCapturing)
            {
                // ֹͣץ����������ͼƫ�ƣ��û������϶����κ�ʱ���
                return;
            }

            var viewWidth = GetViewTimeRange(_timeScale);
            var maxOffset = _currentTimeSeconds - viewWidth;
            var minOffset = Math.Max(0, _currentTimeSeconds - 300);

            _viewOffset = Math.Max(minOffset, Math.Min(maxOffset, _viewOffset));
        }

        private float GetViewTimeRange(float scale)
        {
            // �޸�Ĭ��ʱ�䷶Χ��10���Ϊ5��
            return 30.0f / scale;
        }

        #endregion

        #region �������

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

        #region ����¼�����

        private void OnChartMouseEnter(object sender, EventArgs e)
        {
            if (!Focused)
            {
                Focus();
            }
        }

        private void OnChartMouseMove(object sender, MouseEventArgs e)
        {
            var chartRect = new Rectangle(PaddingLeft, PaddingTop,
                                        Width - PaddingLeft - _paddingRight,
                                        Height - PaddingTop - PaddingBottom);

            if (chartRect.Contains(e.Location))
            {
                if (_isPanning && e.Button == MouseButtons.Left)
                {
                    _lastUserInteraction = DateTime.Now; // ��¼�û�����ʱ��
                    _hasUserInteracted = true; // ����û��н���

                    var deltaX = e.X - _lastMousePosition.X;
                    var timeRange = GetViewTimeRange(_timeScale);
                    var timeDelta = -deltaX * timeRange / chartRect.Width;

                    SetViewOffset(_viewOffset + timeDelta);
                    _lastMousePosition = e.Location;
                    return;
                }

                if (!_isPanning)
                {
                    FindNearestDataPoint(e.Location, chartRect);
                }
            }
            else
            {
                HideTooltip();
            }

            _lastMousePosition = e.Location;
        }

        private void OnChartMouseWheel(object sender, MouseEventArgs e)
        {
            if (!Focused)
            {
                Focus();
            }

            var shouldZoom = (ModifierKeys & Keys.Control) == Keys.Control || !_isPanning;

            if (shouldZoom)
            {
                _lastUserInteraction = DateTime.Now; // ��¼�û�����ʱ��
                _hasUserInteracted = true; // ����û��н���

                var chartRect = new Rectangle(PaddingLeft, PaddingTop,
                                            Width - PaddingLeft - _paddingRight,
                                            Height - PaddingTop - PaddingBottom);

                if (chartRect.Contains(e.Location))
                {
                    var scaleDelta = e.Delta > 0 ? 1.1f : 0.9f;
                    var newScale = _timeScale * scaleDelta;

                    if (newScale >= 0.1f && newScale <= 20.0f)
                    {
                        SetTimeScale(newScale);
                    }
                }
            }
        }

        private void OnChartMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (!Focused)
                {
                    Focus();
                }

                var chartRect = new Rectangle(PaddingLeft, PaddingTop,
                                            Width - PaddingLeft - _paddingRight,
                                            Height - PaddingTop - PaddingBottom);

                if (chartRect.Contains(e.Location))
                {
                    _lastUserInteraction = DateTime.Now; // ��¼�û�����ʱ��
                    _hasUserInteracted = true; // ����û��н���
                    _isPanning = true;
                    _lastMousePosition = e.Location;
                    Cursor = Cursors.Hand;
                    HideTooltip();
                }
            }
        }

        private void OnChartMouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && _isPanning)
            {
                _isPanning = false;
                Cursor = Cursors.Default;

                var timer = new System.Windows.Forms.Timer { Interval = 100 };
                timer.Tick += (s, args) =>
                {
                    timer.Stop();
                    timer.Dispose();
                };
                timer.Start();
            }
        }

        private void OnChartKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.R)
            {
                ResetViewToDefault();
                e.Handled = true;
            }
        }

        #endregion

        #region ���ݵ���Һ���ʾ

        private void FindNearestDataPoint(Point mouseLocation, Rectangle chartRect)
        {
            if (_series.Count == 0) return;

            var viewRange = CalculateViewRange();
            if (viewRange.IsEmpty) return;

            var minDistance = double.MaxValue;
            string bestTooltip = "";
            var found = false;

            foreach (var series in _series)
            {
                if (series.Points.Count == 0) continue;

                foreach (var point in series.Points)
                {
                    if (point.X < viewRange.X || point.X > viewRange.X + viewRange.Width)
                        continue;

                    var screenX = chartRect.X + ((point.X - viewRange.X) / viewRange.Width) * chartRect.Width;
                    var screenY = chartRect.Bottom - (point.Y - viewRange.Y) / viewRange.Height * chartRect.Height;

                    var distance = Math.Sqrt(Math.Pow(mouseLocation.X - screenX, 2) + Math.Pow(mouseLocation.Y - screenY, 2));

                    if (distance < 15 && distance < minDistance)
                    {
                        minDistance = distance;
                        var timeText = FormatTimeLabel(point.X);
                        var dpsText = Common.FormatWithEnglishUnits(point.Y);
                        bestTooltip = $"{series.Name}\nʱ��: {timeText}\nDPS: {dpsText}";
                        found = true;
                    }
                }
            }

            if (found)
            {
                ShowTooltip(bestTooltip, mouseLocation);
            }
            else
            {
                HideTooltip();
            }
        }

        private void ShowTooltip(string text, Point location)
        {
            if (_tooltipText != text)
            {
                _tooltipText = text;
                _showTooltip = true;
                _tooltip.Show(text, this, location.X + 10, location.Y - 30, 3000);
            }
        }

        private void HideTooltip()
        {
            if (_showTooltip)
            {
                _showTooltip = false;
                _tooltip.Hide(this);
            }
        }

        #endregion

        #region ��д������ȷ�����㴦��

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            Focus();
        }

        protected override bool ProcessDialogKey(Keys keyData)
        {
            if (keyData == Keys.R)
            {
                ResetViewToDefault();
                return true;
            }
            return base.ProcessDialogKey(keyData);
        }

        protected override bool IsInputKey(Keys keyData)
        {
            if (keyData == Keys.R)
                return true;
            return base.IsInputKey(keyData);
        }

        #endregion

        #region ���Ʒ���

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;

            g.Clear(BackColor);

            if (_series.Count == 0)
            {
                DrawNoDataMessage(g);
                return;
            }

            var viewRange = CalculateViewRange();
            if (viewRange.IsEmpty) return;

            var chartRect = new Rectangle(PaddingLeft, PaddingTop,
                                        Width - PaddingLeft - _paddingRight,
                                        Height - PaddingTop - PaddingBottom);

            if (_showGrid)
            {
                DrawGrid(g, chartRect, viewRange);
            }

            DrawAxes(g, chartRect, viewRange);

            var clipRect = new Rectangle(chartRect.X - 1, chartRect.Y - 1,
                                        chartRect.Width + 2, chartRect.Height + 2);
            g.SetClip(clipRect);

            DrawDataLines(g, chartRect, viewRange);
            g.ResetClip();

            DrawTitle(g);

            if (_showViewInfo)
            {
                DrawViewInfo(g);
            }

            if (_showLegend && _series.Count > 0)
            {
                DrawLegend(g);
            }
        }

        private void DrawNoDataMessage(Graphics g)
        {
            var message = "��������\n\nʹ�÷�����\n? Ctrl + �����֣�����ʱ����\n? ����϶���ƽ����ͼ\n? R����������ͼ\n? �����ͣ���鿴����";
            using var font = CreateScaledFont("Microsoft YaHei", BaseNoDataFontSize, FontStyle.Regular);
            using var brush = new SolidBrush(_isDarkTheme ? Color.Gray : Color.DarkGray);

            var size = g.MeasureString(message, font);
            var x = (Width - size.Width) / 2;
            var y = (Height - size.Height) / 2;

            g.DrawString(message, font, brush, x, y);
        }

        private RectangleF CalculateViewRange()
        {
            if (_series.Count == 0) return RectangleF.Empty;

            var allPoints = _series.SelectMany(s => s.Points);
            if (!allPoints.Any()) return RectangleF.Empty;

            var minY = 0f;
            var maxY = allPoints.Max(p => p.Y);
            var rangeY = maxY - minY;
            if (rangeY == 0) rangeY = 1;

            var viewWidth = GetViewTimeRange(_timeScale);
            var viewMinX = _viewOffset;

            return new RectangleF(
                viewMinX,
                minY,
                viewWidth,
                rangeY * 1.15f
            );
        }

        private void DrawGrid(Graphics g, Rectangle chartRect, RectangleF viewRange)
        {
            var gridColor = _isDarkTheme ? Color.FromArgb(64, 64, 64) : Color.FromArgb(230, 230, 230);
            using var gridPen = new Pen(gridColor, 1);

            // ��ֱ������ - ���ݶ�̬������_verticalGridLines��������
            // _verticalGridLines��ʾ����������ʵ�ʱ�ǩ��������_verticalGridLines + 1
            for (int i = 0; i <= _verticalGridLines; i++)
            {
                var x = chartRect.X + (float)chartRect.Width * i / _verticalGridLines;
                g.DrawLine(gridPen, x, chartRect.Y, x, chartRect.Bottom);
            }

            // ˮƽ������ - ���̶ֹ���6����(0-5)
            for (int i = 0; i <= 5; i++)
            {
                var y = chartRect.Y + (float)chartRect.Height * i / 5;
                g.DrawLine(gridPen, chartRect.X, y, chartRect.Right, y);
            }
        }

        private void DrawAxes(Graphics g, Rectangle chartRect, RectangleF viewRange)
        {
            var axisColor = _isDarkTheme ? Color.FromArgb(128, 128, 128) : Color.FromArgb(180, 180, 180);
            using var axisPen = new Pen(axisColor, 1);
            using var textBrush = new SolidBrush(ForeColor);

            // Ϊ���ǩʹ�û���ͼ������������С
            using var font = CreateScaledFontForArea("Microsoft YaHei", BaseAxisValueFontSize, chartRect.Width, chartRect.Height);

            g.DrawLine(axisPen, chartRect.X, chartRect.Bottom, chartRect.Right, chartRect.Bottom);
            g.DrawLine(axisPen, chartRect.X, chartRect.Y, chartRect.X, chartRect.Bottom);

            // X��ʱ���ǩ - ���ݶ�̬������_verticalGridLines�������Ʊ�ǩ
            // _verticalGridLines���߶�Ӧ_verticalGridLines + 1����ǩ��
            for (int i = 0; i <= _verticalGridLines; i++)
            {
                var x = chartRect.X + (float)chartRect.Width * i / _verticalGridLines;
                var timeValue = viewRange.X + viewRange.Width * i / _verticalGridLines;
                var text = FormatTimeLabel(timeValue);

                var size = g.MeasureString(text, font);
                g.DrawString(text, font, textBrush, x - size.Width / 2, chartRect.Bottom + 8);
            }

            // Y����ֵ��ǩ - ����Ϊ6����ǩ
            for (int i = 0; i <= 5; i++) // ��4��Ϊ5��6����ǩ(0-5)
            {
                var y = chartRect.Bottom - (float)chartRect.Height * i / 5; // ��ĸ��4��Ϊ5
                var value = viewRange.Y + viewRange.Height * i / 5; // ��ĸ��4��Ϊ5
                var text = Common.FormatWithEnglishUnits(value);

                var size = g.MeasureString(text, font);
                var labelX = Math.Max(5, chartRect.X - size.Width - 8);
                g.DrawString(text, font, textBrush, labelX, y - size.Height / 2);
            }

            if (!string.IsNullOrEmpty(_xAxisLabel))
            {
                using var axisFont = CreateScaledFont("Microsoft YaHei", BaseAxisLabelFontSize);
                var size = g.MeasureString(_xAxisLabel, axisFont);
                var x = chartRect.X + (chartRect.Width - size.Width) / 2;
                var y = chartRect.Bottom + Math.Max(20, 45 * CalculateScaledFontSize(BaseAxisLabelFontSize) / BaseAxisLabelFontSize);
                g.DrawString(_xAxisLabel, axisFont, textBrush, x, y);
            }

            if (!string.IsNullOrEmpty(_yAxisLabel))
            {
                using var axisFont = CreateScaledFont("Microsoft YaHei", BaseAxisLabelFontSize);
                var size = g.MeasureString(_yAxisLabel, axisFont);
                using var matrix = new Matrix();
                matrix.RotateAt(-90, new PointF(20, chartRect.Y + (chartRect.Height + size.Width) / 2));
                g.Transform = matrix;
                g.DrawString(_yAxisLabel, axisFont, textBrush, 20, chartRect.Y + (chartRect.Height + size.Width) / 2);
                g.ResetTransform();
            }
        }

        private string FormatTimeLabel(float seconds)
        {
            if (seconds < 60)
            {
                return $"{seconds:F0}s";
            }
            else
            {
                var minutes = (int)(seconds / 60);
                var remainingSeconds = (int)(seconds % 60);
                return $"{minutes}m{remainingSeconds:D2}s";
            }
        }

        private void DrawDataLines(Graphics g, Rectangle chartRect, RectangleF viewRange)
        {
            foreach (var series in _series)
            {
                if (series.Points.Count < 2) continue;

                using var pen = new Pen(series.Color, series.LineWidth);
                pen.LineJoin = LineJoin.Round;
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;

                var visiblePoints = series.Points
                    .Where(p => p.X >= viewRange.X && p.X <= viewRange.X + viewRange.Width)
                    .Select(p =>
                    {
                        var screenX = chartRect.X + ((p.X - viewRange.X) / viewRange.Width) * chartRect.Width;
                        var screenY = chartRect.Bottom - (p.Y - viewRange.Y) / viewRange.Height * chartRect.Height;

                        screenX = Math.Max(chartRect.X, Math.Min(chartRect.Right, screenX));
                        screenY = Math.Max(chartRect.Y, Math.Min(chartRect.Bottom, screenY));

                        return new PointF(screenX, screenY);
                    }).ToArray();

                if (visiblePoints.Length < 2) continue;

                try
                {
                    if (visiblePoints.Length >= 3)
                    {
                        g.DrawCurve(pen, visiblePoints, 0.6f);
                    }
                    else
                    {
                        g.DrawLines(pen, visiblePoints);
                    }
                }
                catch
                {
                    for (int i = 0; i < visiblePoints.Length - 1; i++)
                    {
                        try
                        {
                            g.DrawLine(pen, visiblePoints[i], visiblePoints[i + 1]);
                        }
                        catch { }
                    }
                }
            }
        }

        private void DrawTitle(Graphics g)
        {
            if (string.IsNullOrEmpty(_titleText)) return;

            using var font = CreateScaledFont("Microsoft YaHei", BaseTitleFontSize, FontStyle.Bold);
            using var brush = new SolidBrush(ForeColor);

            var size = g.MeasureString(_titleText, font);
            var x = (Width - size.Width) / 2;
            var y = 15;

            g.DrawString(_titleText, font, brush, x, y);
        }

        private void DrawViewInfo(Graphics g)
        {
            var info = $"����: {_timeScale:F1}x | ��ǰʱ��: {FormatTimeLabel(_currentTimeSeconds)}";

            using var font = CreateScaledFont("Microsoft YaHei", BaseAxisValueFontSize);
            using var brush = new SolidBrush(_isDarkTheme ? Color.LightGray : Color.DarkGray);

            var size = g.MeasureString(info, font);
            g.DrawString(info, font, brush, Width - size.Width - 10, Height - size.Height - 5);
        }

        private void DrawLegend(Graphics g)
        {
            using var font = CreateScaledFont("Microsoft YaHei", BaseLegendFontSize);
            using var textBrush = new SolidBrush(ForeColor);

            // ���������С����ͼ����ļ��
            var scaledItemHeight = (int)(18 * CalculateScaledFontSize(BaseLegendFontSize) / BaseLegendFontSize);
            var legendHeight = _series.Count * scaledItemHeight + 10;
            var maxTextWidth = _series.Max(s => (int)g.MeasureString(s.Name, font).Width);
            var legendWidth = maxTextWidth + 35;
            var legendX = Width - legendWidth - 15;
            var legendY = PaddingTop + 15;

            var legendBg = _isDarkTheme ? Color.FromArgb(50, 50, 50) : Color.FromArgb(245, 245, 245);
            using var bgBrush = new SolidBrush(legendBg);
            using var borderPen = new Pen(_isDarkTheme ? Color.FromArgb(80, 80, 80) : Color.FromArgb(200, 200, 200), 1);

            var legendRect = new Rectangle(legendX - 8, legendY - 8, legendWidth + 6, legendHeight + 6);
            g.FillRectangle(bgBrush, legendRect);
            g.DrawRectangle(borderPen, legendRect);

            for (int i = 0; i < _series.Count; i++)
            {
                var series = _series[i];
                var y = legendY + i * scaledItemHeight;

                // ���������С����������ϸ
                var lineWidth = Math.Max(2f, 3f * CalculateScaledFontSize(BaseLegendFontSize) / BaseLegendFontSize);
                using var colorPen = new Pen(series.Color, lineWidth);
                g.DrawLine(colorPen, legendX, y + scaledItemHeight / 2, legendX + 20, y + scaledItemHeight / 2);
                g.DrawString(series.Name, font, textBrush, legendX + 25, y + 2);
            }
        }

        #endregion

        #region ��Դ����

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _tooltip?.Dispose();
                _refreshTimer?.Stop();
                _refreshTimer?.Dispose();
            }
            base.Dispose(disposing);
        }

        #endregion

        /// <summary>
        /// ��ȫ����ͼ��״̬�������������ʱ��
        /// </summary>
        public void FullReset()
        {
            // �����������
            _series.Clear();
            _persistentData.Clear();

            // ��������״̬����
            _timeScale = 1.0f;
            _viewOffset = 0.0f;
            _currentTimeSeconds = 0.0f;
            _hasUserInteracted = false;
            _lastUserInteraction = DateTime.MinValue;

            // ֹͣ���ж�ʱ�����������ö�ʱ������
            if (_refreshTimer != null)
            {
                _refreshTimer.Stop();
                // ��Ҫ����Ϊnull�����ֶ�ʱ���������
                AutoRefreshEnabled = false;
            }

            // ǿ���ػ�
            Invalidate();
        }
    }

    /// <summary>
    /// ����ͼ����ϵ��
    /// </summary>
    public class LineChartSeries
    {
        public string Name { get; set; } = "";
        public List<PointF> Points { get; set; } = new();
        public Color Color { get; set; }
        public float LineWidth { get; set; } = 3.5f;
    }
}