using AntdUI;
using StarResonanceDpsAnalysis.Plugin;
using StarResonanceDpsAnalysis.Plugin.DamageStatistics;
using WinFormsControl = System.Windows.Forms.Control;
using WinFormsPanel = System.Windows.Forms.Panel;

namespace StarResonanceDpsAnalysis.Forms
{
    // ����ʾһ�м��ܿ�Ƭ�ļ�ര�ڣ���չʾ AppConfig.Uid �����ݣ�
    public class SkillCardStripForm : BorderlessForm
    {
        private readonly System.Windows.Forms.Timer _refreshTimer;
        private FlowLayoutPanel _flow;
        private readonly List<SkillRotationData> _history = new();
        private readonly Dictionary<ulong, DateTime> _lastUsage = new();

        // ԭʼ��Ƭ�ߴ�
        private const int BASE_CARD_WIDTH = 120;
        private const int BASE_CARD_HEIGHT = 80;
        private const float SCALE = 0.75f; // ��С 25%

        // �����ߴ��������ڱ߾�
        private static int CARD_WIDTH => (int)Math.Round(BASE_CARD_WIDTH * SCALE);
        private static int CARD_HEIGHT => (int)Math.Round(BASE_CARD_HEIGHT * SCALE);
        private static int PX(int v) => Math.Max(1, (int)Math.Round(v * SCALE));

        private const int PAD_L = 8, PAD_T = 5, PAD_R = 8, PAD_B = 5;

        private ulong _uid;

        public SkillCardStripForm()
        {
            // ��ʼ������ UI
            FormGui.SetDefaultGUI(this);
            Text = "���ܿ�Ƭ";
            ShowInTaskbar = true;
            MinimizeBox = false;
            MaximizeBox = false;

            // ����һ�㴰��߶ȣ�Ԥ��ˮƽ�������߶ȣ�����ѹס��Ƭ
            int extraH = SystemInformation.HorizontalScrollBarHeight + 4;
            ClientSize = new Size(800, CARD_HEIGHT + PAD_T + PAD_B + 2 + extraH);

            // ��ʽ�����������棩
            FormGui.SetColorMode(this, AppConfig.IsLight);

            // ������һ��չʾ�������У������Զ�����������ʾˮƽ��������
            _flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoScroll = true,
                Padding = new Padding(PAD_L, PAD_T, PAD_R, PAD_B),
                BackColor = Color.Transparent
            };
            // ͨ�����ø߶Ⱥ͵��в��֣����ⴹֱ����������
            _flow.AutoScrollMargin = new Size(0, 0);

            // ����˫���壬������˸
            TryEnableDoubleBuffer(_flow);

            Controls.Add(_flow);

            // �������������϶�����
            AttachDragHandlers(this);

            // ��ʱˢ�£��Զ�������أ�
            _refreshTimer = new System.Windows.Forms.Timer { Interval = 500, Enabled = false };
            _refreshTimer.Tick += RefreshTimer_Tick;

            Load += SkillCardStripForm_Load;
            FormClosed += (_, __) => _refreshTimer?.Stop();
        }

        private void SkillCardStripForm_Load(object? sender, EventArgs e)
        {
            _uid = AppConfig.Uid; // ����ʾ�� UID �ļ���
            ResetStateAndUi();
            _refreshTimer.Start(); // �Զ��������
        }

        private void RefreshTimer_Tick(object? sender, EventArgs e)
        {
            // ���� AppConfig.Uid ��̬�л�����������ɫ����õ�UID��
            if (_uid != AppConfig.Uid)
            {
                _uid = AppConfig.Uid;
                ResetStateAndUi();
            }

            if (_uid == 0) return; // δ���� UID �򲻸���

            try
            {
                var playerData = StatisticData._manager.GetOrCreate(_uid);
                var skillSummaries = playerData.GetSkillSummaries(
                    topN: null,
                    orderByTotalDesc: false,
                    filterType: StarResonanceDpsAnalysis.Core.SkillType.Damage);

                if (skillSummaries == null || skillSummaries.Count == 0) return;

                bool added = false;
                foreach (var s in skillSummaries)
                {
                    if (!s.LastTime.HasValue) continue;
                    var lastTime = s.LastTime.Value;
                    var id = s.SkillId;

                    if (!_lastUsage.TryGetValue(id, out var prev) || lastTime > prev)
                    {
                        _lastUsage[id] = lastTime;

                        var data = new SkillRotationData
                        {
                            SkillId = id,
                            SkillName = s.SkillName,
                            UseTime = lastTime,
                            SequenceNumber = _history.Count + 1
                        };
                        _history.Add(data);
                        AddCard(data);
                        added = true;
                    }
                }

                // ������ʷ���ȣ�������࿨Ƭ
                const int MAX_HISTORY = 200;
                while (_history.Count > MAX_HISTORY && _flow.Controls.Count > 0)
                {
                    _history.RemoveAt(0);
                    var first = _flow.Controls[0];
                    _flow.Controls.RemoveAt(0);
                    first.Dispose();
                }

                if (added) UpdateSequenceLabels();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SkillCardStrip ˢ���쳣: {ex.Message}");
            }
        }

        private void ResetStateAndUi()
        {
            _flow.SuspendLayout();
            try
            {
                _history.Clear();
                _lastUsage.Clear();
                foreach (WinFormsControl c in _flow.Controls) c.Dispose();
                _flow.Controls.Clear();
            }
            finally
            {
                _flow.ResumeLayout(true);
            }
        }

        private void RebuildAllCards()
        {
            _flow.SuspendLayout();
            try
            {
                foreach (WinFormsControl c in _flow.Controls) c.Dispose();
                _flow.Controls.Clear();

                foreach (var data in _history)
                {
                    _flow.Controls.Add(CreateCard(data));
                }
            }
            finally
            {
                _flow.ResumeLayout(true);
            }
        }

        private void AddCard(SkillRotationData data)
        {
            if (IsDisposed) return;
            var card = CreateCard(data);
            BeginInvoke(new Action(() =>
            {
                _flow.Controls.Add(card);
                ScrollToEnd(); // �����Զ���������
            }));
        }

        private void ScrollToEnd()
        {
            try
            {
                // ͨ������ AutoScrollPosition �� HorizontalScroll.Value ����ĩβ
                var h = _flow.HorizontalScroll;
                int target = Math.Max(h.Minimum, h.Maximum - h.LargeChange + 1);
                if (target < h.Minimum) target = h.Minimum;
                if (target != h.Value)
                {
                    h.Value = target;
                }
                _flow.AutoScrollPosition = new Point(h.Maximum, 0);
            }
            catch { }
        }

        private WinFormsControl CreateCard(SkillRotationData skill)
        {
            var card = new WinFormsPanel
            {
                Size = new Size(CARD_WIDTH, CARD_HEIGHT),
                Margin = new Padding(PX(2)),
                BackColor = AppConfig.IsLight ? Color.FromArgb(250, 250, 250) : Color.FromArgb(45, 45, 45),
                BorderStyle = BorderStyle.FixedSingle,
                Tag = skill.SkillId.ToString()
            };

            var nameLabel = new AntdUI.Label
            {
                Text = skill.SkillName.Length > 8 ? skill.SkillName[..8] + "..." : skill.SkillName,
                Location = new Point(PX(5), PX(5)),
                Size = new Size(CARD_WIDTH - PX(10), PX(20)),
                Font = new Font("Microsoft YaHei", 8 * SCALE, FontStyle.Bold),
                TextAlign = ContentAlignment.TopCenter
            };
            var timeLabel = new AntdUI.Label
            {
                Text = skill.UseTime.ToString("HH:mm:ss"),
                Location = new Point(PX(5), PX(25)),
                Size = new Size(CARD_WIDTH - PX(10), PX(15)),
                Font = new Font("Microsoft YaHei", 7 * SCALE),
                ForeColor = Color.Gray,
                TextAlign = ContentAlignment.TopCenter
            };
            var seqLabel = new AntdUI.Label
            {
                Text = $"#{skill.SequenceNumber}",
                Location = new Point(PX(5), PX(45)),
                Size = new Size(CARD_WIDTH - PX(10), PX(15)),
                Font = new Font("Microsoft YaHei", 7 * SCALE),
                ForeColor = Color.Blue,
                TextAlign = ContentAlignment.TopCenter
            };

            card.Controls.Add(nameLabel);
            card.Controls.Add(timeLabel);
            card.Controls.Add(seqLabel);

            if (skill.SequenceNumber > 1)
            {
                var prev = _history.FirstOrDefault(s => s.SequenceNumber == skill.SequenceNumber - 1);
                if (prev != null)
                {
                    var interval = (skill.UseTime - prev.UseTime).TotalSeconds;
                    var intervalLabel = new AntdUI.Label
                    {
                        Text = $"+{interval:F1}s",
                        Location = new Point(PX(5), PX(60)),
                        Size = new Size(CARD_WIDTH - PX(10), PX(15)),
                        Font = new Font("Microsoft YaHei", 6 * SCALE),
                        ForeColor = Color.Orange,
                        TextAlign = ContentAlignment.TopCenter
                    };
                    card.Controls.Add(intervalLabel);
                }
            }

            // �����ڿ�Ƭ�����ӿؼ����϶�����
            AttachDragHandlers(card);

            return card;
        }

        private void UpdateSequenceLabels()
        {
            var cards = _flow.Controls.OfType<WinFormsPanel>().ToList();
            for (int i = 0; i < cards.Count && i < _history.Count; i++)
            {
                var seqLabel = cards[i].Controls
                    .OfType<AntdUI.Label>()
                    .FirstOrDefault(l => l.Text.StartsWith("#"));
                if (seqLabel != null)
                {
                    seqLabel.Text = $"#{_history[i].SequenceNumber}";
                }
            }
        }

        private static void TryEnableDoubleBuffer(FlowLayoutPanel panel)
        {
            try
            {
                typeof(System.Windows.Forms.Panel).InvokeMember("DoubleBuffered",
                    System.Reflection.BindingFlags.SetProperty |
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic,
                    null, panel, new object[] { true });
            }
            catch { }
        }

        // ======== �϶�����֧�� ========
        private void AttachDragHandlers(WinFormsControl ctrl)
        {
            if (ctrl == null) return;
            ctrl.MouseDown += Drag_MouseDown;
            foreach (WinFormsControl child in ctrl.Controls)
            {
                AttachDragHandlers(child);
            }
        }

        private void Drag_MouseDown(object? sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                FormManager.ReleaseCapture();
                FormManager.SendMessage(this.Handle, FormManager.WM_NCLBUTTONDOWN, FormManager.HTCAPTION, 0);
            }
        }

        private class SkillRotationData
        {
            public ulong SkillId { get; set; }
            public string SkillName { get; set; } = string.Empty;
            public DateTime UseTime { get; set; }
            public int SequenceNumber { get; set; }
        }
    }
}
