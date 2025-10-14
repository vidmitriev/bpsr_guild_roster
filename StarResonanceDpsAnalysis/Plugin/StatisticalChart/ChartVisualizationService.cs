using StarResonanceDpsAnalysis.Plugin.Charts;
using StarResonanceDpsAnalysis.Plugin.DamageStatistics;
using System.Timers;

namespace StarResonanceDpsAnalysis.Plugin
{
    /// <summary>
    /// ͼ�����ù����� - ͳһ�������ͼ���Ĭ������
    /// </summary>
    public static class ChartConfigManager
    {
        // ͳһ��Ĭ�ϳ���
        public const string EMPTY_TEXT = "";
        public const bool HIDE_LEGEND = false;
        public const bool SHOW_GRID = true;
        public const bool SHOW_VIEW_INFO = false;
        public const bool AUTO_SCALE_FONT = false;
        public const bool PRESERVE_VIEW = true;
        public const int REFRESH_INTERVAL = 1000;
        public const int MIN_WIDTH = 450;
        public const int MIN_HEIGHT = 150;

        public static readonly Font DefaultFont = new("΢���ź�", 10, FontStyle.Regular);

        /// <summary>
        /// ͳһӦ��ͼ��Ĭ������
        /// </summary>
        public static T ApplySettings<T>(T chart) where T : UserControl
        {
            // ͨ�ÿؼ�����
            chart.Dock = DockStyle.Fill;

            // ����ͼ������Ӧ���ض�����
            switch (chart)
            {
                case FlatLineChart lineChart:
                    ApplyLineChartSettings(lineChart);
                    break;
                case FlatBarChart barChart:
                    ApplyBarChartSettings(barChart);
                    break;
                case FlatPieChart pieChart:
                    ApplyPieChartSettings(pieChart);
                    break;
                case FlatScatterChart scatterChart:
                    ApplyScatterChartSettings(scatterChart);
                    break;
            }

            return chart;
        }

        private static void ApplyLineChartSettings(FlatLineChart chart)
        {
            chart.TitleText = EMPTY_TEXT;
            chart.XAxisLabel = EMPTY_TEXT;
            chart.YAxisLabel = EMPTY_TEXT;
            chart.ShowLegend = HIDE_LEGEND;
            chart.ShowGrid = SHOW_GRID;
            chart.ShowViewInfo = SHOW_VIEW_INFO;
            chart.AutoScaleFont = AUTO_SCALE_FONT;
            chart.PreserveViewOnDataUpdate = PRESERVE_VIEW;
            chart.IsDarkTheme = !AppConfig.IsLight;
            chart.MinimumSize = new Size(MIN_WIDTH, MIN_HEIGHT);
            chart.Font = DefaultFont;
        }

        private static void ApplyBarChartSettings(FlatBarChart chart)
        {
            chart.TitleText = EMPTY_TEXT;
            chart.IsDarkTheme = !AppConfig.IsLight;
        }

        private static void ApplyPieChartSettings(FlatPieChart chart)
        {
            chart.TitleText = EMPTY_TEXT;
            chart.IsDarkTheme = !AppConfig.IsLight;
            chart.ShowLabels = true;
            chart.ShowPercentages = true;
        }

        private static void ApplyScatterChartSettings(FlatScatterChart chart)
        {
            chart.TitleText = EMPTY_TEXT;
            chart.XAxisLabel = EMPTY_TEXT;
            chart.YAxisLabel = EMPTY_TEXT;
            chart.ShowLegend = true;
            chart.ShowGrid = SHOW_GRID;
            chart.IsDarkTheme = !AppConfig.IsLight;
        }
    }

    /// <summary>
    /// ͼ��������Դ
    /// </summary>
    public enum ChartDataSource
    {
        Current = 0,   // ��ǰս�������Σ�
        FullRecord = 1 // ȫ�̣��Ự��
    }

    /// <summary>
    /// ͼ����������
    /// </summary>
    public enum ChartDataType
    {
        Damage = 0,      // �˺�
        Healing = 1,     // ���� 
        TakenDamage = 2  // ����
    }

    /// <summary>
    /// ʵʱͼ����ӻ�����
    /// </summary>
    public static class ChartVisualizationService
    {
        #region ���ݴ洢
        // ===== ����ʷ������Դ���룺Current �� FullRecord ����һ�� =====
        private static readonly Dictionary<ulong, List<(DateTime Time, double Dps)>> _dpsHistoryCurrent = new();
        private static readonly Dictionary<ulong, List<(DateTime Time, double Hps)>> _hpsHistoryCurrent = new();
        private static readonly Dictionary<ulong, List<(DateTime Time, double TakenDps)>> _takenDpsHistoryCurrent = new();

        private static readonly Dictionary<ulong, List<(DateTime Time, double Dps)>> _dpsHistoryFull = new();
        private static readonly Dictionary<ulong, List<(DateTime Time, double Hps)>> _hpsHistoryFull = new();
        private static readonly Dictionary<ulong, List<(DateTime Time, double TakenDps)>> _takenDpsHistoryFull = new();

        private static DateTime? _currentCombatStartTime;
        private static DateTime? _fullCombatStartTime;

        private static readonly List<WeakReference> _registeredCharts = new();

        private const int MAX_HISTORY_POINTS = 500;
        private const double INACTIVE_TIMEOUT_SECONDS = 2.0;

        public static bool IsCapturing { get; private set; } = false;

        // ���־ɵ�Ĭ������Դ������δ��ʽָ����Դ�� API��
        public static ChartDataSource DataSource { get; private set; } = ChartDataSource.Current;

        // Ƶ�ʽ�����������ͼ��ͬʱ�����ظ�����
        private static DateTime _lastUpdateAt = DateTime.MinValue;
        private const int MIN_UPDATE_INTERVAL_MS = 200; // ���β������ټ�� 200ms

        // ��̨������ʱ������ʹû�д��κ�ͼ��Ҳ�������
        private static System.Timers.Timer? _samplingTimer;

        // ��ǰս������0��>0���ı��ؼ�⣬�Զ��зֵ��λỰ����Ϊ����ս��ʱ�ӣ�
        private static bool _wasInCombat = false;

        // ===== ȫ�̼�ʱ���ʼ��㣺ʹ�á���֡����ʵʱֵ������ʹ���ۼ�ƽ���� =====
        private static readonly Dictionary<ulong, (ulong Total, DateTime Ts)> _fullLastDamage = new();
        private static readonly Dictionary<ulong, (ulong Total, DateTime Ts)> _fullLastHealing = new();
        private static readonly Dictionary<ulong, (ulong Total, DateTime Ts)> _fullLastTaken = new();
        #endregion

        #region ���ݸ���
        /// <summary>
        /// �л�ͼ��������Դ�����Զ������ʷ���������ݻ�������
        /// </summary>
        public static void SetDataSource(ChartDataSource source, bool clearHistory = true)
        {
            if (DataSource == source) return;
            DataSource = source;
            if (clearHistory)
            {
                if (source == ChartDataSource.Current)
                    ClearCurrentHistory();
                else
                    ClearFullHistory();
            }
        }

        // �ڲ�����ָ����ʷ�������һ�����ݵ㣨������������
        private static void AddDataPoint<T>(Dictionary<ulong, List<(DateTime, T)>> history, ulong playerId, T value)
        {
            var now = DateTime.Now;

            if (!history.TryGetValue(playerId, out var playerHistory))
            {
                playerHistory = new List<(DateTime, T)>();
                history[playerId] = playerHistory;
            }

            // ȷ����ֵ�Ǹ�
            var safeValue = value is double d ? (T)(object)Math.Max(0, d) : value;
            playerHistory.Add((now, safeValue));

            if (playerHistory.Count > MAX_HISTORY_POINTS)
                playerHistory.RemoveAt(0);
        }

        // === ��ǰս����������ݵ㣬�����״γ�����ֵʱ�趨��ʼʱ�� ===
        public static void AddDpsDataPointCurrent(ulong playerId, double dps)
        {
            if (_currentCombatStartTime is null && dps > 0)
                _currentCombatStartTime = DateTime.Now;
            AddDataPoint(_dpsHistoryCurrent, playerId, dps);
        }
        public static void AddHpsDataPointCurrent(ulong playerId, double hps)
        {
            if (_currentCombatStartTime is null && hps > 0)
                _currentCombatStartTime = DateTime.Now;
            AddDataPoint(_hpsHistoryCurrent, playerId, hps);
        }
        public static void AddTakenDpsDataPointCurrent(ulong playerId, double takenDps)
        {
            if (_currentCombatStartTime is null && takenDps > 0)
                _currentCombatStartTime = DateTime.Now;
            AddDataPoint(_takenDpsHistoryCurrent, playerId, takenDps);
        }

        // === ȫ�̣�������ݵ㣬�����״γ�����ֵʱ�趨��ʼʱ�� ===
        public static void AddDpsDataPointFull(ulong playerId, double dps)
        {
            if (_fullCombatStartTime is null && dps > 0)
                _fullCombatStartTime = DateTime.Now;
            AddDataPoint(_dpsHistoryFull, playerId, dps);
        }
        public static void AddHpsDataPointFull(ulong playerId, double hps)
        {
            if (_fullCombatStartTime is null && hps > 0)
                _fullCombatStartTime = DateTime.Now;
            AddDataPoint(_hpsHistoryFull, playerId, hps);
        }
        public static void AddTakenDpsDataPointFull(ulong playerId, double takenDps)
        {
            if (_fullCombatStartTime is null && takenDps > 0)
                _fullCombatStartTime = DateTime.Now;
            AddDataPoint(_takenDpsHistoryFull, playerId, takenDps);
        }

        /// <summary>
        /// ͳһ������ͬʱ���� Current �� FullRecord ������ʷ��
        /// </summary>
        public static void UpdateAllDataPoints()
        {
            // ��δ���ڲ���״̬������ F9 ��ִ�й�ֹͣ����գ����򲻽����κθ��£�������պ����ϱ�����
            if (!IsCapturing) return;

            var now = DateTime.Now;
            if ((now - _lastUpdateAt).TotalMilliseconds < MIN_UPDATE_INTERVAL_MS)
                return;
            _lastUpdateAt = now;

            // === Current������ StatisticData ��ʵʱֵ ===
            var playersCurrent = StatisticData._manager.GetPlayersWithCombatData();
            foreach (var player in playersCurrent)
                player.UpdateRealtimeStats();

            // �á�ս��ʱ��״̬���ж��µ�һ����������Ϊʵʱ���ڹ��������
            var nowInCombat = StatisticData._manager.IsInCombat;
            if (!_wasInCombat && nowInCombat)
            {
                // ��ս����ʼ����������ʷ��ʱ�����������ʱ�Ӷ���
                ClearCurrentHistory();
                _currentCombatStartTime = DateTime.Now - StatisticData._manager.GetCombatDuration();
            }

            foreach (var player in playersCurrent)
            {
                AddDpsDataPointCurrent(player.Uid, player.DamageStats.RealtimeValue);
                AddHpsDataPointCurrent(player.Uid, player.HealingStats.RealtimeValue);
                AddTakenDpsDataPointCurrent(player.Uid, player.TakenStats.RealtimeValue);
            }
            _wasInCombat = nowInCombat;

            // === FullRecord����Ϊ��¼��ʵʱ�仯�ʡ�����֣����������ۼ�ƽ������ֵ ===
            // ��ȡ��ǰ�ۼ�����
            var totals = FullRecord.GetPlayersWithTotals(includeZero: true);
            foreach (var p in totals)
            {
                // Damage -> ʵʱDPS����֣�
                if (_fullLastDamage.TryGetValue(p.Uid, out var lastDmg))
                {
                    var dt = (now - lastDmg.Ts).TotalSeconds;
                    if (dt > 0)
                    {
                        var delta = (long)p.TotalDamage - (long)lastDmg.Total;
                        var dps = delta > 0 ? delta / dt : 0.0;
                        AddDpsDataPointFull(p.Uid, Math.Round(dps, 2, MidpointRounding.AwayFromZero));
                    }
                    _fullLastDamage[p.Uid] = ((ulong)Math.Max(0, (long)p.TotalDamage), now);
                }
                else
                {
                    _fullLastDamage[p.Uid] = (p.TotalDamage, now);
                }

                // Healing -> ʵʱHPS����֣�
                if (_fullLastHealing.TryGetValue(p.Uid, out var lastHeal))
                {
                    var dt = (now - lastHeal.Ts).TotalSeconds;
                    if (dt > 0)
                    {
                        var delta = (long)p.TotalHealing - (long)lastHeal.Total;
                        var hps = delta > 0 ? delta / dt : 0.0;
                        AddHpsDataPointFull(p.Uid, Math.Round(hps, 2, MidpointRounding.AwayFromZero));
                    }
                    _fullLastHealing[p.Uid] = ((ulong)Math.Max(0, (long)p.TotalHealing), now);
                }
                else
                {
                    _fullLastHealing[p.Uid] = (p.TotalHealing, now);
                }

                // Taken -> ʵʱ����ÿ�루��֣�
                if (_fullLastTaken.TryGetValue(p.Uid, out var lastTaken))
                {
                    var dt = (now - lastTaken.Ts).TotalSeconds;
                    if (dt > 0)
                    {
                        var delta = (long)p.TakenDamage - (long)lastTaken.Total;
                        var tps = delta > 0 ? delta / dt : 0.0;
                        AddTakenDpsDataPointFull(p.Uid, Math.Round(tps, 2, MidpointRounding.AwayFromZero));
                    }
                    _fullLastTaken[p.Uid] = ((ulong)Math.Max(0, (long)p.TakenDamage), now);
                }
                else
                {
                    _fullLastTaken[p.Uid] = (p.TakenDamage, now);
                }
            }

            CheckAndAddZeroValues();
        }

        private static void CheckAndAddZeroValues()
        {
            HashSet<ulong> activeCurrent = StatisticData._manager.GetPlayersWithCombatData().Select(p => p.Uid).ToHashSet();
            HashSet<ulong> activeFull = FullRecord.GetPlayersWithTotals(includeZero: false).Select(p => p.Uid).ToHashSet();

            var now = DateTime.Now;

            // Ϊ�ǻ�Ծ���Ҳ�� 0 ֵ����ֹ��ʱ��ͣ������һ��ֵ��
            CheckHistoryForZeroValues(_dpsHistoryCurrent, activeCurrent, now, (id, _) => AddDpsDataPointCurrent(id, 0));
            CheckHistoryForZeroValues(_hpsHistoryCurrent, activeCurrent, now, (id, _) => AddHpsDataPointCurrent(id, 0));
            CheckHistoryForZeroValues(_takenDpsHistoryCurrent, activeCurrent, now, (id, _) => AddTakenDpsDataPointCurrent(id, 0));

            CheckHistoryForZeroValues(_dpsHistoryFull, activeFull, now, (id, _) => AddDpsDataPointFull(id, 0));
            CheckHistoryForZeroValues(_hpsHistoryFull, activeFull, now, (id, _) => AddHpsDataPointFull(id, 0));
            CheckHistoryForZeroValues(_takenDpsHistoryFull, activeFull, now, (id, _) => AddTakenDpsDataPointFull(id, 0));
        }

        private static void CheckHistoryForZeroValues<T>(Dictionary<ulong, List<(DateTime Time, T Value)>> history,
            HashSet<ulong> activePlayerIds, DateTime now, Action<ulong, T> addZeroValue)
            where T : struct, IComparable<T>
        {
            var zero = default(T);
            foreach (var playerId in history.Keys.ToList())
            {
                if (activePlayerIds.Contains(playerId)) continue;

                var playerHistory = history[playerId];
                if (playerHistory.Count > 0)
                {
                    var lastRecord = playerHistory.Last();
                    var timeSinceLastRecord = (now - lastRecord.Time).TotalSeconds;

                    if (timeSinceLastRecord > INACTIVE_TIMEOUT_SECONDS && lastRecord.Value.CompareTo(zero) > 0)
                        addZeroValue(playerId, zero);
                }
            }
        }

        public static void ClearAllHistory()
        {
            ClearCurrentHistory();
            ClearFullHistory();
        }

        public static void ClearCurrentHistory()
        {
            _dpsHistoryCurrent.Clear();
            _hpsHistoryCurrent.Clear();
            _takenDpsHistoryCurrent.Clear();
            _currentCombatStartTime = null;
        }

        public static void ClearFullHistory()
        {
            _dpsHistoryFull.Clear();
            _hpsHistoryFull.Clear();
            _takenDpsHistoryFull.Clear();
            _fullCombatStartTime = null;

            // ͬ����ղ�ֿ��գ������Ự���˲ʱ��ֵ
            _fullLastDamage.Clear();
            _fullLastHealing.Clear();
            _fullLastTaken.Clear();
        }

        public static void OnCombatEnd()
        {
            // ��ս������ʱѹ�� 0 ֵ���Ա�������Ȼ����
            foreach (var playerId in _dpsHistoryCurrent.Keys.ToList())
            {
                var history = _dpsHistoryCurrent[playerId];
                if (history.Count > 0 && history.Last().Dps > 0)
                    AddDpsDataPointCurrent(playerId, 0);
            }
            foreach (var playerId in _hpsHistoryCurrent.Keys.ToList())
            {
                var history = _hpsHistoryCurrent[playerId];
                if (history.Count > 0 && history.Last().Hps > 0)
                    AddHpsDataPointCurrent(playerId, 0);
            }
            foreach (var playerId in _takenDpsHistoryCurrent.Keys.ToList())
            {
                var history = _takenDpsHistoryCurrent[playerId];
                if (history.Count > 0 && history.Last().TakenDps > 0)
                    AddTakenDpsDataPointCurrent(playerId, 0);
            }

            // ȫ��Ҳ�� 0 ֵ������ͼ���ν�
            foreach (var playerId in _dpsHistoryFull.Keys.ToList())
            {
                var history = _dpsHistoryFull[playerId];
                if (history.Count > 0 && history.Last().Dps > 0)
                    AddDpsDataPointFull(playerId, 0);
            }
            foreach (var playerId in _hpsHistoryFull.Keys.ToList())
            {
                var history = _hpsHistoryFull[playerId];
                if (history.Count > 0 && history.Last().Hps > 0)
                    AddHpsDataPointFull(playerId, 0);
            }
            foreach (var playerId in _takenDpsHistoryFull.Keys.ToList())
            {
                var history = _takenDpsHistoryFull[playerId];
                if (history.Count > 0 && history.Last().TakenDps > 0)
                    AddTakenDpsDataPointFull(playerId, 0);
            }
        }
        #endregion

        #region ͼ����
        /// <summary>
        /// ͨ�ô�������
        /// </summary>
        /// <typeparam name="T">ͼ��ؼ����ͣ��̳��� UserControl</typeparam>
        /// <param name="size">ͼ��ĳ�ʼ��С</param>
        /// <param name="customConfig">��ѡ���Զ������ûص������޸�ͼ��ؼ��ĸ��ֲ���</param>
        /// <returns>�Ѵ�����Ӧ��Ĭ�����õ�ͼ��ʵ��</returns>
        private static T CreateChart<T>(Size size, Action<T> customConfig = null) where T : UserControl, new()
        {
            var chart = new T { Size = size };
            ChartConfigManager.ApplySettings(chart); // Ӧ��ͳһ��ͼ������
            customConfig?.Invoke(chart); // ִ���Զ�������
            return chart;
        }

        /// <summary>
        /// ���� DPS ��������ͼ��Ĭ��ʹ��ȫ�� DataSource��
        /// </summary>
        public static FlatLineChart CreateDpsTrendChart(int width = 800, int height = 400, ulong? specificPlayerId = null)
        {
            var chart = CreateChart<FlatLineChart>(new Size(width, height));

            RegisterChart(chart); // ע��ͼ���Ա�ͳһ����

            if (IsCapturing) // ����ǰ�ڲ������ݣ������Զ�ˢ��
                chart.StartAutoRefresh(ChartConfigManager.REFRESH_INTERVAL);

            RefreshDpsTrendChart(chart, specificPlayerId); // �����ʼ����
            return chart;
        }

        /// <summary>
        /// Ϊָ������Դ���� DPS ����ͼ��Current / FullRecord����
        /// </summary>
        public static FlatLineChart CreateDpsTrendChartForSource(ChartDataSource source, int width = 800, int height = 400, ulong? specificPlayerId = null)
        {
            var chart = CreateChart<FlatLineChart>(new Size(width, height));
            RegisterChart(chart);
            if (IsCapturing) chart.StartAutoRefresh(ChartConfigManager.REFRESH_INTERVAL);
            RefreshDpsTrendChart(chart, specificPlayerId, ChartDataType.Damage, source);
            return chart;
        }

        /// <summary>
        /// ??????????????????FlatPieChart??
        /// </summary>
        public static FlatPieChart CreateSkillDamagePieChart(ulong playerId, int width = 400, int height = 400)
        {
            var chart = CreateChart<FlatPieChart>(new Size(width, height));
            RefreshSkillDamagePieChart(chart, playerId); // ��ʼˢ��
            return chart;
        }

        /// <summary>
        /// �������� DPS ����ͼ��FlatBarChart��
        /// </summary>
        public static FlatBarChart CreateTeamDpsBarChart(int width = 600, int height = 400)
        {
            var chart = CreateChart<FlatBarChart>(new Size(width, height));
            RefreshTeamDpsBarChart(chart); // ��ʼˢ��
            return chart;
        }

        /// <summary>
                /// ���� DPS ɢ��ͼ��FlatScatterChart��
/// </summary>
        public static FlatScatterChart CreateDpsRadarChart(int width = 400, int height = 400)
        {
            var chart = CreateChart<FlatScatterChart>(new Size(width, height));
            RefreshDpsRadarChart(chart); // ��ʼˢ��
            return chart;
        }

        /// <summary>
        /// �����˺����Ͷѵ�����ͼ��FlatBarChart��
        /// </summary>
        public static FlatBarChart CreateDamageTypeStackedChart(int width = 600, int height = 400)
        {
            var chart = CreateChart<FlatBarChart>(new Size(width, height));
            RefreshDamageTypeStackedChart(chart); // ��ʼˢ��
            return chart;
        }

        #endregion

        #region ͼ��ˢ��
        /// <summary>
        /// ˢ�� DPS ����ͼ���ݣ�֧�ֵ���/�����Լ���ͬ�������ͣ�Ĭ��ʹ��ȫ�� DataSource��
        /// </summary>
        public static void RefreshDpsTrendChart(FlatLineChart chart, ulong? specificPlayerId = null, ChartDataType dataType = ChartDataType.Damage)
            => RefreshDpsTrendChart(chart, specificPlayerId, dataType, DataSource);

        /// <summary>
        /// ��ָ������Դˢ�����ߣ�Current/FullRecord����
        /// </summary>
        public static void RefreshDpsTrendChart(FlatLineChart chart, ulong? specificPlayerId, ChartDataType dataType, ChartDataSource source)
        {
            // ��¼ͼ��״̬
            var timeScale = chart.GetTimeScale();
            var viewOffset = chart.GetViewOffset();
            var hadData = chart.HasData();

            chart.ClearSeries();

            // ѡ���Ӧ��ʷ
            Dictionary<ulong, List<(DateTime Time, double Value)>> historyData;
            DateTime? startTs;
            if (source == ChartDataSource.FullRecord)
            {
                historyData = dataType switch
                {
                    ChartDataType.Healing => _hpsHistoryFull.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Select(item => (item.Time, (double)item.Hps)).ToList()),
                    ChartDataType.TakenDamage => _takenDpsHistoryFull.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Select(item => (item.Time, (double)item.TakenDps)).ToList()),
                    _ => _dpsHistoryFull.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Select(item => (item.Time, (double)item.Dps)).ToList()),
                };
                startTs = _fullCombatStartTime;
            }
            else
            {
                historyData = dataType switch
                {
                    ChartDataType.Healing => _hpsHistoryCurrent.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Select(item => (item.Time, (double)item.Hps)).ToList()),
                    ChartDataType.TakenDamage => _takenDpsHistoryCurrent.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Select(item => (item.Time, (double)item.TakenDps)).ToList()),
                    _ => _dpsHistoryCurrent.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Select(item => (item.Time, (double)item.Dps)).ToList()),
                };
                // �����á�������ս��ʱ�����Ƶ����Ŀ�ʼʱ�䣬��֤�뵥�μ�ʱһ��
                startTs = (_currentCombatStartTime != null) ? _currentCombatStartTime : DateTime.Now - StatisticData._manager.GetCombatDuration();
            }

            // ��û���κ���ʷ����ʼʱ��δ֪��������պ󣩣�ֱ�ӷ��أ����֡��������ݡ���̬��
            if (historyData.Count == 0 || startTs == null)
            {
                chart.Invalidate();
                return;
            }

            var startTime = startTs.Value;

            if (specificPlayerId.HasValue)
            {
                RefreshSinglePlayerChart(chart, historyData, specificPlayerId.Value, startTime);
            }
            else
            {
                RefreshMultiPlayerChart(chart, historyData, startTime);
            }

            // �ָ���ͼ
            if (hadData && chart.HasUserInteracted())
            {
                chart.SetTimeScale(timeScale);
                chart.SetViewOffset(viewOffset);
            }
        }

        private static void RefreshSinglePlayerChart(FlatLineChart chart, Dictionary<ulong, List<(DateTime Time, double Value)>> historyData,
            ulong playerId, DateTime startTime)
        {
            if (historyData.TryGetValue(playerId, out var playerHistory) && playerHistory.Count > 0)
            {
                var points = ConvertToPoints(playerHistory, startTime);
                if (points.Count > 0)
                {
                    // ȷ���� 0 �뿪ʼ���׸��������� 0s����һ�� (0,0)
                    if (points[0].X > 0f)
                    {
                        points.Insert(0, new PointF(0f, 0f));
                    }
                    chart.AddSeries("", points);
                }
            }
        }

        private static void RefreshMultiPlayerChart(FlatLineChart chart, Dictionary<ulong, List<(DateTime Time, double Value)>> historyData,
            DateTime startTime)
        {
            foreach (var (playerId, history) in historyData.OrderBy(x => x.Key))
            {
                if (history.Count == 0) continue;

                var points = ConvertToPoints(history, startTime);
                if (points.Count > 0)
                    chart.AddSeries("", points);
            }
        }

        private static List<PointF> ConvertToPoints(List<(DateTime Time, double Value)> history, DateTime startTime)
        {
            return history.Select(h => new PointF(
                (float)(h.Time - startTime).TotalSeconds,
                (float)h.Value
            )).ToList();
        }

        public static void RefreshSkillDamagePieChart(FlatPieChart chart, ulong playerId, ChartDataType dataType = ChartDataType.Damage)
        {
            chart.ClearData();

            try
            {
                // �����������ͻ�ȡ��Ӧ�ļ�������
                var skillData = dataType switch
                {
                    ChartDataType.Healing => StatisticData._manager.GetPlayerSkillSummaries(playerId, topN: 8, orderByTotalDesc: true, StarResonanceDpsAnalysis.Core.SkillType.Heal),
                    ChartDataType.TakenDamage => StatisticData._manager.GetPlayerTakenDamageSummaries(playerId, topN: 8, orderByTotalDesc: true),
                    _ => StatisticData._manager.GetPlayerSkillSummaries(playerId, topN: 8, orderByTotalDesc: true, StarResonanceDpsAnalysis.Core.SkillType.Damage)
                };

                if (skillData.Count == 0) return;

                var pieData = skillData.Select(s => (
                    Label: $"{s.SkillName}: {Common.FormatWithEnglishUnits(s.Total)}",
                    Value: (double)s.Total
                )).ToList();

                chart.SetData(pieData);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ˢ�¼����˺���ͼʱ����: {ex.Message}");
            }
        }

        public static void RefreshTeamDpsBarChart(FlatBarChart chart)
        {
            chart.ClearData();
            var players = StatisticData._manager.GetPlayersWithCombatData().ToList();
            if (players.Count == 0) return;

            var barData = players
                .OrderByDescending(p => p.GetTotalDps())
                .Select(p => (Label: "", Value: p.GetTotalDps()))
                .ToList();

            chart.SetData(barData);
        }

        public static void RefreshDpsRadarChart(FlatScatterChart chart)
        {
            chart.ClearSeries();
            var players = StatisticData._manager.GetPlayersWithCombatData().Take(5).ToList();
            if (players.Count == 0) return;

            foreach (var player in players)
            {
                var totalDps = player.GetTotalDps();
                var critRate = player.DamageStats.GetCritRate();
                var points = new List<PointF> { new((float)critRate, (float)totalDps) };
                chart.AddSeries("", points);
            }
        }

        public static void RefreshDamageTypeStackedChart(FlatBarChart chart)
        {
            chart.ClearData();
            var players = StatisticData._manager.GetPlayersWithCombatData()
                .OrderByDescending(p => p.DamageStats.Total)
                .Take(6)
                .ToList();

            if (players.Count == 0) return;

            var barData = players.Select(p => (Label: "", Value: (double)p.DamageStats.Total)).ToList();
            chart.SetData(barData);
        }
        #endregion

        #region ͼ�����
        public static void RegisterChart(FlatLineChart chart)
        {
            lock (_registeredCharts)
            {
                _registeredCharts.RemoveAll(wr => !wr.IsAlive);
                _registeredCharts.Add(new WeakReference(chart));
            }
        }

        public static void StopAllChartsAutoRefresh()
        {
            IsCapturing = false;
            ExecuteOnRegisteredCharts(chart => chart.StopAutoRefresh());

            try { _samplingTimer?.Stop(); } catch { }
            try { _samplingTimer?.Dispose(); } catch { }
            _samplingTimer = null;
        }

        public static void StartAllChartsAutoRefresh(int intervalMs = 1000)
        {
            IsCapturing = true;
            ExecuteOnRegisteredCharts(chart => chart.StartAutoRefresh(intervalMs));

            // ������̨��������ʹ��δ���κ�ͼ��Ҳ��������ݣ�
            if (_samplingTimer == null)
            {
                _samplingTimer = new System.Timers.Timer(Math.Max(200, intervalMs));
                _samplingTimer.AutoReset = true;
                _samplingTimer.Elapsed += (_, __) =>
                {
                    try { UpdateAllDataPoints(); }
                    catch (Exception ex) { Console.WriteLine($"Chart sampling error: {ex.Message}"); }
                };
            }
            _samplingTimer.Interval = Math.Max(200, intervalMs);
            _samplingTimer.Start();

            // ͬ��һ�Ρ�ս��״̬�������������ʱ����
            _wasInCombat = StatisticData._manager.IsInCombat;
        }

        public static void FullResetAllCharts()
        {
            ClearAllHistory();
            ExecuteOnRegisteredCharts(chart =>
            {
                try
                {
                    // ֹͣˢ�²����������������ͼ
                    chart.StopAutoRefresh();
                    chart.ClearSeries();
                    chart.FullReset();
                    chart.Invalidate(); // �����ػ��̬
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"FullReset chart error: {ex.Message}");
                }
            });
        }

        private static void ExecuteOnRegisteredCharts(Action<FlatLineChart> action)
        {
            lock (_registeredCharts)
            {
                foreach (var weakRef in _registeredCharts.ToList())
                {
                    if (weakRef.IsAlive && weakRef.Target is FlatLineChart chart)
                    {
                        try { action(chart); }
                        catch (Exception ex) { Console.WriteLine($"ͼ�����ִ�г���: {ex.Message}"); }
                    }
                }
                _registeredCharts.RemoveAll(wr => !wr.IsAlive);
            }
        }
        #endregion

        #region ��������
        public static bool HasDataToVisualize() =>
            StatisticData._manager.GetPlayersWithCombatData().Any();

        public static double GetCombatDurationSeconds(ChartDataSource source = ChartDataSource.Current) =>
            (source == ChartDataSource.Current ? _currentCombatStartTime : _fullCombatStartTime)?.Let(start => (DateTime.Now - start).TotalSeconds) ?? 0;

        public static int GetDpsHistoryPointCount() =>
            _dpsHistoryCurrent.Sum(kvp => kvp.Value.Count) + _dpsHistoryFull.Sum(kvp => kvp.Value.Count);
        #endregion
    }

    /// <summary>
    /// ��չ���߷���
    /// </summary>
    public static class Extensions
    {
        public static TResult Let<T, TResult>(this T obj, Func<T, TResult> func) => func(obj);
    }
}