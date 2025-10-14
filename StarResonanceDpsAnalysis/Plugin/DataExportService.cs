using ClosedXML.Excel;
using StarResonanceDpsAnalysis.Plugin.DamageStatistics;
using System.Text;

namespace StarResonanceDpsAnalysis.Plugin
{
    /// <summary>
    /// ���ݵ�������֧��Excel��CSV��ʽ
    /// </summary>
    public static class DataExportService
    {
        #region Excel����

        /// <summary>
        /// ����DPS���ݵ�Excel�ļ�
        /// </summary>
        /// <param name="players">��������б�</param>
        /// <param name="includeSkillDetails">�Ƿ������������</param>
        /// <returns>�Ƿ񵼳��ɹ�</returns>
        public static bool ExportToExcel(List<PlayerData> players, bool includeSkillDetails = true)
        {
            try
            {
                using var saveDialog = new SaveFileDialog
                {
                    Filter = "Excel�ļ� (*.xlsx)|*.xlsx",
                    DefaultExt = "xlsx",
                    FileName = $"DPSͳ��_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.xlsx",
                    Title = "����DPSͳ������"
                };

                if (saveDialog.ShowDialog() != DialogResult.OK)
                    return false;

                using var workbook = new XLWorkbook();

                // �������������
                CreatePlayerOverviewSheet(workbook, players);

                if (includeSkillDetails)
                {
                    // �������������
                    CreateSkillDetailsSheet(workbook, players);

                    // �����ŶӼ���ͳ�Ʊ�
                    CreateTeamSkillStatsSheet(workbook, players);
                }

                workbook.SaveAs(saveDialog.FileName);

                MessageBox.Show($"�����ѳɹ�������:\n{saveDialog.FileName}", "�����ɹ�",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"����Excel�ļ�ʱ��������:\n{ex.Message}", "����ʧ��",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>
        /// �����������������
        /// </summary>
        private static void CreatePlayerOverviewSheet(XLWorkbook workbook, List<PlayerData> players)
        {
            var worksheet = workbook.Worksheets.Add("�������");

            // ���ñ�ͷ
            var headers = new[]
            {
                "����ǳ�", "ְҵ", "ս��", "���˺�", "��DPS", "�����˺�", "�����˺�",
                "������", "������", "˲ʱDPS��ֵ", "������", "��HPS", "�����˺�", "���д���"
            };

            // д���ͷ
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(1, i + 1).Value = headers[i];
                worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightBlue;
            }

            // д������
            int row = 2;
            foreach (var player in players.OrderByDescending(p => p.DamageStats.Total))
            {
                worksheet.Cell(row, 1).Value = player.Nickname;
                worksheet.Cell(row, 2).Value = player.Profession;
                worksheet.Cell(row, 3).Value = player.CombatPower;
                worksheet.Cell(row, 4).Value = (double)player.DamageStats.Total;
                worksheet.Cell(row, 5).Value = Math.Round(player.GetTotalDps(), 1);
                worksheet.Cell(row, 6).Value = (double)player.DamageStats.Critical;
                worksheet.Cell(row, 7).Value = (double)player.DamageStats.Lucky;
                worksheet.Cell(row, 8).Value = $"{player.DamageStats.GetCritRate()}%";
                worksheet.Cell(row, 9).Value = $"{player.DamageStats.GetLuckyRate()}%";
                worksheet.Cell(row, 10).Value = (double)player.DamageStats.RealtimeMax;
                worksheet.Cell(row, 11).Value = (double)player.HealingStats.Total;
                worksheet.Cell(row, 12).Value = Math.Round(player.GetTotalHps(), 1);
                worksheet.Cell(row, 13).Value = (double)player.TakenDamage;
                worksheet.Cell(row, 14).Value = player.DamageStats.CountTotal;

                row++;
            }

            // �Զ������п�
            worksheet.ColumnsUsed().AdjustToContents();

            // ���ɸѡ
            worksheet.Range(1, 1, row - 1, headers.Length).SetAutoFilter();
        }

        /// <summary>
        /// �����������鹤����
        /// </summary>
        private static void CreateSkillDetailsSheet(XLWorkbook workbook, List<PlayerData> players)
        {
            var worksheet = workbook.Worksheets.Add("��������");

            // ���ñ�ͷ
            var headers = new[]
            {
                "����ǳ�", "��������", "���˺�", "���д���", "ƽ���˺�",
                "������", "������", "����DPS", "�˺�ռ��"
            };

            // д���ͷ
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(1, i + 1).Value = headers[i];
                worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGreen;
            }

            // д������
            int row = 2;
            foreach (var player in players.OrderByDescending(p => p.DamageStats.Total))
            {
                var skills = StatisticData._manager.GetPlayerSkillSummaries(
                    player.Uid, topN: null, orderByTotalDesc: true);

                foreach (var skill in skills)
                {
                    worksheet.Cell(row, 1).Value = player.Nickname;
                    worksheet.Cell(row, 2).Value = skill.SkillName;
                    worksheet.Cell(row, 3).Value = (double)skill.Total;
                    worksheet.Cell(row, 4).Value = skill.HitCount;
                    worksheet.Cell(row, 5).Value = Math.Round(skill.AvgPerHit, 1);
                    worksheet.Cell(row, 6).Value = $"{skill.CritRate * 100:F1}%";
                    worksheet.Cell(row, 7).Value = $"{skill.LuckyRate * 100:F1}%";
                    worksheet.Cell(row, 8).Value = Math.Round(skill.TotalDps, 1);
                    worksheet.Cell(row, 9).Value = $"{skill.ShareOfTotal * 100:F1}%";

                    row++;
                }
            }

            // �Զ������п�
            worksheet.ColumnsUsed().AdjustToContents();

            // ���ɸѡ
            if (row > 2)
                worksheet.Range(1, 1, row - 1, headers.Length).SetAutoFilter();
        }

        /// <summary>
        /// �����ŶӼ���ͳ�ƹ�����
        /// </summary>
        private static void CreateTeamSkillStatsSheet(XLWorkbook workbook, List<PlayerData> players)
        {
            var worksheet = workbook.Worksheets.Add("�ŶӼ���ͳ��");

            // ��ȡ�ŶӼ�������
            var teamSkills = StatisticData._manager.GetTeamTopSkillsByTotal(50);

            // ���ñ�ͷ
            var headers = new[]
            {
                "��������", "���˺�", "�����д���", "�Ŷ�ռ��"
            };

            // д���ͷ
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(1, i + 1).Value = headers[i];
                worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightYellow;
            }

            // �������˺����ڰٷֱȼ���
            ulong totalTeamDamage = (ulong)teamSkills.Sum(s => (double)s.Total);

            // д������
            int row = 2;
            foreach (var skill in teamSkills)
            {
                worksheet.Cell(row, 1).Value = skill.SkillName;
                worksheet.Cell(row, 2).Value = (double)skill.Total;
                worksheet.Cell(row, 3).Value = skill.HitCount;
                worksheet.Cell(row, 4).Value = totalTeamDamage > 0 ?
                    $"{((double)skill.Total / totalTeamDamage) * 100:F1}%" : "0%";

                row++;
            }

            // �Զ������п�
            worksheet.ColumnsUsed().AdjustToContents();

            // ���ɸѡ
            if (row > 2)
                worksheet.Range(1, 1, row - 1, headers.Length).SetAutoFilter();
        }

        #endregion

        #region CSV����

        /// <summary>
        /// ����DPS���ݵ�CSV�ļ�
        /// </summary>
        /// <param name="players">��������б�</param>
        /// <returns>�Ƿ񵼳��ɹ�</returns>
        public static bool ExportToCsv(List<PlayerData> players)
        {
            try
            {
                using var saveDialog = new SaveFileDialog
                {
                    Filter = "CSV�ļ� (*.csv)|*.csv",
                    DefaultExt = "csv",
                    FileName = $"DPSͳ��_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.csv",
                    Title = "����DPSͳ������"
                };

                if (saveDialog.ShowDialog() != DialogResult.OK)
                    return false;

                var csv = new StringBuilder();

                // ���BOM��ȷ��Excel��ȷ��ʾ����
                csv.Append('\uFEFF');

                // CSV��ͷ
                csv.AppendLine("����ǳ�,ְҵ,ս��,���˺�,��DPS,�����˺�,�����˺�,������,������,˲ʱDPS��ֵ,������,��HPS,�����˺�,���д���");

                // ������
                foreach (var player in players.OrderByDescending(p => p.DamageStats.Total))
                {
                    csv.AppendLine($"\"{EscapeCsvField(player.Nickname)}\"," +
                                 $"\"{EscapeCsvField(player.Profession)}\"," +
                                 $"{player.CombatPower}," +
                                 $"{player.DamageStats.Total}," +
                                 $"{player.GetTotalDps():F1}," +
                                 $"{player.DamageStats.Critical}," +
                                 $"{player.DamageStats.Lucky}," +
                                 $"{player.DamageStats.GetCritRate()}%," +
                                 $"{player.DamageStats.GetLuckyRate()}%," +
                                 $"{player.DamageStats.RealtimeMax}," +
                                 $"{player.HealingStats.Total}," +
                                 $"{player.GetTotalHps():F1}," +
                                 $"{player.TakenDamage}," +
                                 $"{player.DamageStats.CountTotal}");
                }

                File.WriteAllText(saveDialog.FileName, csv.ToString(), Encoding.UTF8);

                MessageBox.Show($"�����ѳɹ�������:\n{saveDialog.FileName}", "�����ɹ�",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"����CSV�ļ�ʱ��������:\n{ex.Message}", "����ʧ��",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>
        /// ת��CSV�ֶ��е������ַ�
        /// </summary>
        private static string EscapeCsvField(string field)
        {
            if (string.IsNullOrEmpty(field))
                return "";

            // ����������š����Ż��з�����Ҫ�����Ű�Χ��ת���ڲ�����
            if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
            {
                return field.Replace("\"", "\"\"");
            }

            return field;
        }

        #endregion

        #region ��ͼ����

        /// <summary>
        /// ���洰�ڽ�ͼ
        /// </summary>
        /// <param name="form">Ҫ��ͼ�Ĵ���</param>
        /// <returns>�Ƿ񱣴�ɹ�</returns>
        public static bool SaveScreenshot(Form form)
        {
            try
            {
                using var saveDialog = new SaveFileDialog
                {
                    Filter = "PNGͼƬ (*.png)|*.png|JPEGͼƬ (*.jpg)|*.jpg",
                    DefaultExt = "png",
                    FileName = $"DPS��ͼ_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.png",
                    Title = "����DPS�����ͼ"
                };

                if (saveDialog.ShowDialog() != DialogResult.OK)
                    return false;

                // �����봰�ڴ�С��ͬ��λͼ
                var bounds = form.Bounds;
                using var bitmap = new System.Drawing.Bitmap(bounds.Width, bounds.Height);
                using var graphics = System.Drawing.Graphics.FromImage(bitmap);

                // ��ȡ��������
                graphics.CopyFromScreen(bounds.Location, System.Drawing.Point.Empty, bounds.Size);

                // �����ļ���չ������
                var extension = Path.GetExtension(saveDialog.FileName).ToLower();
                var format = extension switch
                {
                    ".jpg" or ".jpeg" => System.Drawing.Imaging.ImageFormat.Jpeg,
                    _ => System.Drawing.Imaging.ImageFormat.Png
                };

                bitmap.Save(saveDialog.FileName, format);

                MessageBox.Show($"��ͼ�ѳɹ����浽:\n{saveDialog.FileName}", "��ͼ�ɹ�",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"�����ͼʱ��������:\n{ex.Message}", "��ͼʧ��",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        #endregion

        #region ��������

        /// <summary>
        /// ��ȡ��ǰ��ս�����ݵ�����б�
        /// </summary>
        /// <returns>��������б�</returns>
        public static List<PlayerData> GetCurrentPlayerData()
        {
            return StatisticData._manager
                .GetPlayersWithCombatData()
                .ToList();
        }

        /// <summary>
        /// ����Ƿ������ݿɵ���
        /// </summary>
        /// <returns>�Ƿ�������</returns>
        public static bool HasDataToExport()
        {
            return GetCurrentPlayerData().Count > 0;
        }

        #endregion
    }
}