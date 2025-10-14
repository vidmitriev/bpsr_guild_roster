using AntdUI;
using StarResonanceDpsAnalysis.Plugin;
using StarResonanceDpsAnalysis.Plugin.Charts;
using StarResonanceDpsAnalysis.Plugin.DamageStatistics;
using SystemPanel = System.Windows.Forms.Panel;

namespace StarResonanceDpsAnalysis.Forms
{
    /// <summary>
    /// ʵʱͼ���� - ʹ�ñ�ƽ���Զ���ͼ��ؼ����Զ���������ͼ��
    /// </summary>
    public partial class RealtimeChartsForm : BorderlessForm
    {
        private Tabs _tabControl;
        private FlatLineChart _dpsTrendChart;
        private FlatPieChart _skillPieChart;
        private FlatBarChart _teamDpsChart;
        private FlatScatterChart _multiDimensionChart;
        private FlatBarChart _damageTypeChart;
        private Dropdown _playerSelector;

        // ���ư�ť
        private AntdUI.Button _refreshButton;
        private AntdUI.Button _closeButton;
        private AntdUI.Button _autoRefreshToggle;

        // �Զ�ˢ�����
        private System.Windows.Forms.Timer _autoRefreshTimer;
        private bool _autoRefreshEnabled = false;

        // �����϶����
        private bool _isDragging = false;
        private Point _dragStartPoint;
        private SystemPanel _draggablePanel;

        public RealtimeChartsForm()
        {
            InitializeComponent();
            FormGui.SetDefaultGUI(this);

            Text = "ʵʱͼ����ӻ�";
            Size = new Size(1000, 700);
            StartPosition = FormStartPosition.CenterScreen;

            // ���ñ�׼����
            Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular);

            InitializeControls();
            InitializeAutoRefreshTimer();

            // Ӧ�õ�ǰ����
            RefreshChartsTheme();

            // �Զ���������ͼ��
            LoadAllCharts();

            // Ĭ�������Զ�ˢ��
            EnableAutoRefreshByDefault();
        }

        private void InitializeComponent()
        {
            SuspendLayout();
            // 
            // RealtimeChartsForm
            // 
            ClientSize = new Size(1000, 700);
            Name = "RealtimeChartsForm";
            Load += RealtimeChartsForm_Load;
            ResumeLayout(false);
        }

        private void InitializeControls()
        {
            // �������ư�ť��壨���϶���
            _draggablePanel = new SystemPanel
            {
                Height = 50,
                Dock = DockStyle.Top,
                Padding = new Padding(10, 5, 10, 5),
                Cursor = Cursors.SizeAll // ��ʾ���ƶ����
            };

            // Ϊ�϶�����������¼�
            _draggablePanel.MouseDown += DraggablePanel_MouseDown;
            _draggablePanel.MouseMove += DraggablePanel_MouseMove;
            _draggablePanel.MouseUp += DraggablePanel_MouseUp;

            _refreshButton = new AntdUI.Button
            {
                Text = "�ֶ�ˢ��",
                Type = TTypeMini.Primary,
                Size = new Size(80, 35),
                Location = new Point(10, 8),
                Font = Font
            };
            _refreshButton.Click += RefreshButton_Click;

            _autoRefreshToggle = new AntdUI.Button
            {
                Text = "�Զ�ˢ��: ��", // Ĭ����ʾΪ����״̬
                Type = TTypeMini.Primary, // Ĭ��ʹ��Primary��ʽ
                Size = new Size(100, 35),
                Location = new Point(100, 8),
                Font = Font
            };
            _autoRefreshToggle.Click += AutoRefreshToggle_Click;

            _closeButton = new AntdUI.Button
            {
                Text = "�ر�",
                Type = TTypeMini.Default,
                Size = new Size(60, 35),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(_draggablePanel.Width - 70, 8),
                Font = Font
            };
            _closeButton.Click += CloseButton_Click;

            _draggablePanel.Controls.Add(_refreshButton);
            _draggablePanel.Controls.Add(_autoRefreshToggle);
            _draggablePanel.Controls.Add(_closeButton);

            // ����ѡ��ؼ�
            _tabControl = new Tabs
            {
                Dock = DockStyle.Fill,
                Font = Font
            };

            // ���TabPage - ���ı�����
            _tabControl.Pages.Add(new AntdUI.TabPage
            {
                Text = "DPS����ͼ",
                Font = Font
            });
            _tabControl.Pages.Add(new AntdUI.TabPage
            {
                Text = "����ռ��ͼ",
                Font = Font
            });
            _tabControl.Pages.Add(new AntdUI.TabPage
            {
                Text = "�Ŷ�DPS�Ա�",
                Font = Font
            });
            _tabControl.Pages.Add(new AntdUI.TabPage
            {
                Text = "��ά�ȶԱ�",
                Font = Font
            });
            _tabControl.Pages.Add(new AntdUI.TabPage
            {
                Text = "�˺��ֲ�ͼ",
                Font = Font
            });

            // ׼����ҳ������
            for (int i = 0; i < 5; i++)
            {
                var panel = new SystemPanel
                {
                    Dock = DockStyle.Fill,
                    BackColor = AppConfig.IsLight ? Color.White : Color.FromArgb(31, 31, 31)
                };
                _tabControl.Pages[i].Controls.Add(panel);
            }

            // Ϊ����ռ��ͼҳ��������ѡ����
            var skillChartPage = _tabControl.Pages[1];
            var skillChartPanel = skillChartPage.Controls[0] as SystemPanel;

            var playerSelectorPanel = new SystemPanel
            {
                Height = 50,
                Dock = DockStyle.Top,
                Padding = new Padding(10)
            };

            var playerLabel = new AntdUI.Label
            {
                Text = "ѡ����ң�",
                Location = new Point(10, 15),
                AutoSize = true,
                Font = Font
            };

            _playerSelector = new Dropdown
            {
                Location = new Point(90, 10),
                Size = new Size(200, 30),
                Font = Font
            };
            _playerSelector.SelectedValueChanged += PlayerSelector_SelectedValueChanged;

            playerSelectorPanel.Controls.Add(playerLabel);
            playerSelectorPanel.Controls.Add(_playerSelector);
            skillChartPanel.Controls.Add(playerSelectorPanel);

            Controls.Add(_tabControl);
            Controls.Add(_draggablePanel);
        }

        #region �����϶��¼�����

        private void DraggablePanel_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _isDragging = true;
                _dragStartPoint = e.Location;
                _draggablePanel.Cursor = Cursors.Hand;
            }
        }

        private void DraggablePanel_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging && e.Button == MouseButtons.Left)
            {
                // �����ƶ�����
                var deltaX = e.Location.X - _dragStartPoint.X;
                var deltaY = e.Location.Y - _dragStartPoint.Y;

                // �ƶ�����
                this.Location = new Point(this.Location.X + deltaX, this.Location.Y + deltaY);
            }
        }

        private void DraggablePanel_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _isDragging = false;
                _draggablePanel.Cursor = Cursors.SizeAll;
            }
        }

        #endregion

        /// <summary>
        /// Ĭ�������Զ�ˢ��
        /// </summary>
        private void EnableAutoRefreshByDefault()
        {
            _autoRefreshEnabled = true;
            _autoRefreshTimer.Enabled = true;
            _autoRefreshToggle.Text = "�Զ�ˢ��: ��";
            _autoRefreshToggle.Type = TTypeMini.Primary;
        }

        /// <summary>
        /// �Զ���������ͼ��
        /// </summary>
        private void LoadAllCharts()
        {
            try
            {
                // ����DPS����ͼ���Ƴ�������ƣ�
                var dpsTrendPanel = _tabControl.Pages[0].Controls[0] as SystemPanel;
                _dpsTrendChart = ChartVisualizationService.CreateDpsTrendChart();
                dpsTrendPanel.Controls.Add(_dpsTrendChart);

                // ���ؼ���ռ��ͼ
                var skillChartPanel = _tabControl.Pages[1].Controls[0] as SystemPanel;
                UpdatePlayerSelector();
                var selectedPlayer = _playerSelector.SelectedValue as PlayerSelectorItem;
                ulong playerId = selectedPlayer?.Uid ?? 0;
                _skillPieChart = ChartVisualizationService.CreateSkillDamagePieChart(playerId);
                skillChartPanel.Controls.Add(_skillPieChart);

                // �����Ŷ�DPS�Ա�ͼ
                var teamDpsPanel = _tabControl.Pages[2].Controls[0] as SystemPanel;
                _teamDpsChart = ChartVisualizationService.CreateTeamDpsBarChart();
                teamDpsPanel.Controls.Add(_teamDpsChart);

                // ���ض�ά�ȶԱ�ͼ
                var multiDimensionPanel = _tabControl.Pages[3].Controls[0] as SystemPanel;
                _multiDimensionChart = ChartVisualizationService.CreateDpsRadarChart();
                multiDimensionPanel.Controls.Add(_multiDimensionChart);

                // �����˺��ֲ�ͼ
                var damageTypePanel = _tabControl.Pages[4].Controls[0] as SystemPanel;
                _damageTypeChart = ChartVisualizationService.CreateDamageTypeStackedChart();
                damageTypePanel.Controls.Add(_damageTypeChart);

                // ��ʼˢ������ͼ������
                RefreshAllCharts();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"����ͼ��ʱ����: {ex.Message}");
                MessageBox.Show($"����ͼ��ʱ����: {ex.Message}", "����", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void InitializeAutoRefreshTimer()
        {
            _autoRefreshTimer = new System.Windows.Forms.Timer
            {
                Interval = 100, // 0.1�� (100����) ��Ƶˢ��
                Enabled = false
            };
            _autoRefreshTimer.Tick += AutoRefreshTimer_Tick;
        }

        #region �¼�����

        private void RefreshButton_Click(object sender, EventArgs e)
        {
            RefreshAllCharts();

            // ��ʾˢ��״̬
            _refreshButton.Text = "ˢ����...";
            _refreshButton.Enabled = false;

            var resetTimer = new System.Windows.Forms.Timer { Interval = 300 };
            resetTimer.Tick += (s, args) =>
            {
                _refreshButton.Text = "�ֶ�ˢ��";
                _refreshButton.Enabled = true;
                resetTimer.Stop();
                resetTimer.Dispose();
            };
            resetTimer.Start();
        }

        private void AutoRefreshToggle_Click(object sender, EventArgs e)
        {
            _autoRefreshEnabled = !_autoRefreshEnabled;
            _autoRefreshTimer.Enabled = _autoRefreshEnabled;

            _autoRefreshToggle.Text = $"�Զ�ˢ��: {(_autoRefreshEnabled ? "��" : "��")}";
            _autoRefreshToggle.Type = _autoRefreshEnabled ? TTypeMini.Primary : TTypeMini.Default;
        }

        private void AutoRefreshTimer_Tick(object sender, EventArgs e)
        {
            RefreshAllCharts();
        }

        private void CloseButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void PlayerSelector_SelectedValueChanged(object sender, ObjectNEventArgs e)
        {
            if (_playerSelector.SelectedValue is PlayerSelectorItem item && _skillPieChart != null)
            {
                ChartVisualizationService.RefreshSkillDamagePieChart(_skillPieChart, item.Uid);
            }
        }

        #endregion

        private void RefreshAllCharts()
        {
            try
            {
                // �������ݵ�
                ChartVisualizationService.UpdateAllDataPoints();

                // ˢ������ͼ�������û���¼��ʧ
                if (_dpsTrendChart != null)
                {
                    ChartVisualizationService.RefreshDpsTrendChart(_dpsTrendChart, null, ChartDataType.Damage);
                    _dpsTrendChart.ReloadPersistentData(); // ���¼������ݷ�ֹ��ʧ
                }

                if (_skillPieChart != null)
                {
                    var selectedPlayer = _playerSelector.SelectedValue as PlayerSelectorItem;
                    ChartVisualizationService.RefreshSkillDamagePieChart(_skillPieChart, selectedPlayer?.Uid ?? 0);
                }

                if (_teamDpsChart != null)
                    ChartVisualizationService.RefreshTeamDpsBarChart(_teamDpsChart);

                if (_multiDimensionChart != null)
                    ChartVisualizationService.RefreshDpsRadarChart(_multiDimensionChart);

                if (_damageTypeChart != null)
                    ChartVisualizationService.RefreshDamageTypeStackedChart(_damageTypeChart);

                // �������ѡ����
                UpdatePlayerSelector();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ˢ��ͼ��ʱ����: {ex.Message}");
            }
        }

        private void UpdatePlayerSelector()
        {
            var players = StatisticData._manager.GetPlayersWithCombatData().ToList();

            // ���浱ǰѡ��
            var currentSelection = _playerSelector.SelectedValue as PlayerSelectorItem;

            _playerSelector.Items.Clear();

            foreach (var player in players)
            {
                var displayName = string.IsNullOrEmpty(player.Nickname) ? $"���{player.Uid}" : player.Nickname;
                var item = new PlayerSelectorItem { Uid = player.Uid, DisplayName = displayName };
                _playerSelector.Items.Add(item);

                // �ָ�ѡ���Ĭ��ѡ���һ��
                if ((currentSelection != null && currentSelection.Uid == player.Uid) ||
                    (currentSelection == null && _playerSelector.Items.Count == 1))
                {
                    _playerSelector.SelectedValue = item;
                }
            }
        }

        /// <summary>
        /// ˢ��ͼ������
        /// </summary>
        public void RefreshChartsTheme()
        {
            var isDark = !AppConfig.IsLight;

            // ���ô�������
            FormGui.SetColorMode(this, AppConfig.IsLight);

            // ��������ͼ������
            if (_dpsTrendChart != null)
                _dpsTrendChart.IsDarkTheme = isDark;

            if (_skillPieChart != null)
                _skillPieChart.IsDarkTheme = isDark;

            if (_teamDpsChart != null)
                _teamDpsChart.IsDarkTheme = isDark;

            if (_multiDimensionChart != null)
                _multiDimensionChart.IsDarkTheme = isDark;

            if (_damageTypeChart != null)
                _damageTypeChart.IsDarkTheme = isDark;
        }

        /// <summary>
        /// �������ͼ������
        /// </summary>
        public void ClearAllChartData()
        {
            _dpsTrendChart?.ClearSeries();
            _skillPieChart?.ClearData();
            _teamDpsChart?.ClearData();
            _multiDimensionChart?.ClearSeries();
            _damageTypeChart?.ClearData();
            _playerSelector?.Items.Clear();
        }

        /// <summary>
        /// �ֶ�ˢ������ͼ��
        /// </summary>
        public void ManualRefreshCharts()
        {
            RefreshAllCharts();
        }

        /// <summary>
        /// �����Զ�ˢ�¼��
        /// </summary>
        public void SetAutoRefreshInterval(int milliseconds)
        {
            if (_autoRefreshTimer != null)
            {
                _autoRefreshTimer.Interval = Math.Max(50, milliseconds); // ��С50���룬֧�ָ���Ƶ��
            }
        }

        /// <summary>
        /// ��ȡ��ǰ�Զ�ˢ��״̬
        /// </summary>
        public bool IsAutoRefreshEnabled => _autoRefreshEnabled;

        /// <summary>
        /// ��ȡ��ǰˢ�¼��
        /// </summary>
        public int GetRefreshInterval => _autoRefreshTimer?.Interval ?? 100;

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _autoRefreshTimer?.Stop();
            _autoRefreshTimer?.Dispose();
            base.OnFormClosed(e);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            // ���ڼ��غ��Զ�ˢ��һ��ͼ��
            if (_dpsTrendChart != null)
            {
                RefreshAllCharts();
            }
        }

        private void RealtimeChartsForm_Load(object sender, EventArgs e)
        {

        }
    }

    /// <summary>
    /// ���ѡ������
    /// </summary>
    public class PlayerSelectorItem
    {
        public ulong Uid { get; set; }
        public string DisplayName { get; set; } = "";

        public override string ToString()
        {
            return DisplayName;
        }
    }
}