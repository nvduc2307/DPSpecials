using Autodesk.Revit.UI;
using DPSpecial.Tools.ECP.ECPCreateScheduleOrderDetail.model;
using DPSpecial.Tools.ECP.ECPSchedule.ECPCreateSchedule.model;
using DPSpecial.Tools.ECP.ECPCreateScheduleOrderDetail.view;
using DPSpecial.Tools.ECP.ECPCreateScheduleOrderDetail.viewModel;
using DPSpecial.Utils;

namespace DPSpecial.Tools.ECP.ECPCreateScheduleOrderDetail.action
{
    public class ECPCreateScheduleOrderDetailAction
    {
        private readonly UIDocument _uidocument;
        private readonly Document _document;
        private readonly int? _zoneId;
        private readonly ECPCreateScheduleOrderDetailVM _viewModel;
        private readonly ECPCreateScheduleOrderDetailView _view;

        // order/rows are optional: without them the screen falls back to the sample content (standalone command).
        public ECPCreateScheduleOrderDetailAction(UIDocument uidocument, ECPOrderScheduleModel order = null, List<ECPOrderDetailRowModel> rows = null, string branchOffice = null, int? zoneId = null)
        {
            _uidocument = uidocument;
            _document = _uidocument.Document;
            _zoneId = zoneId;

            var header = BuildHeader(_document, order, branchOffice, zoneId);

            _viewModel = new ECPCreateScheduleOrderDetailVM
            {
                Header = header,
                SaveCommand = new RelayCommand(_Save),
                ExportSvgCommand = new RelayCommand(_ExportSvg),
                AttachCommand = new RelayCommand(_ExportPdf),
                CancelCommand = new RelayCommand(_Cancel),
            };
            foreach (var row in rows ?? CreateSampleRows())
                _viewModel.Rows.Add(row);
            _view = new ECPCreateScheduleOrderDetailView { DataContext = _viewModel };
        }

        // Header of one zone: the saved one (ProjectInformation) if there is any, otherwise the defaults,
        // with the auto-mapped values refreshed from the zone / schedule row. Also used by the order slip export.
        public static ECPOrderDetailHeaderModel BuildHeader(Document document, ECPOrderScheduleModel order, string branchOffice, int? zoneId)
        {
            // A header saved for this zone (on ProjectInformation) wins over the defaults below.
            var saved = zoneId.HasValue ? ECPOrderDetailHeaderStore.Find(document, zoneId.Value) : null;
            var header = saved ?? CreateSampleHeader();
            if (order != null)
            {
                // Auto-mapped values (read-only on screen) always follow the current zone / schedule / model.
                header.OrderNo = order.OrderNo;
                header.PropertyRegNo = order.PropertyRegNo;
                header.PropertyDetail = order.PropertyDetail;
                header.PropertyName = order.PropertyName;
                header.DealerName = order.DealerName;
                header.DesiredDate = order.DesiredDeliveryDate;
                header.ActualCount = order.ActualSheets;
                header.BaseArea = order.BaseAreaM2;
                header.BaseWeight = order.BaseWeight;
                header.WorkArea = order.WorkAreaM2;
                header.WorkWeight = order.WorkWeight;

                // Remaining fields are only initialised the first time, so the user's edits are not overwritten.
                if (saved == null)
                {
                    header.BranchOffice = branchOffice ?? string.Empty;
                    header.SiteArrivalDate = order.SiteArrivalDate;
                    header.FactoryShipDate = order.FactoryShipScheduledDate;
                    header.ProcessStartDate = order.ProcessShipScheduledDate;
                }
            }
            return header;
        }

        // Closing the window (キャンセル or the X) saves the header too, like the schedule window does on 戻る.
        public void Execute()
        {
            _view.ShowDialog();
            SaveHeader();
        }

        private void SaveHeader()
        {
            if (_zoneId.HasValue)
                ECPOrderDetailHeaderStore.Save(_document, _zoneId.Value, _viewModel.Header, _viewModel.Rows);
        }

        // 保存: saves the header now and keeps the window open.
        private void _Save()
        {
            if (!_zoneId.HasValue)
            {
                IO.ShowWarning("No zone is selected, so the header cannot be saved.");
                return;
            }
            try
            {
                SaveHeader();
                IO.ShowInfo("Saved.");
            }
            catch (Exception ex)
            {
                IO.ShowWarning(ex.Message);
            }
        }

        // Exports the 縦切図 shapes as SVG files named by type (製品番号) into a folder the user picks.
        private void _ExportSvg()
        {
            using var dialog = new System.Windows.Forms.FolderBrowserDialog { Description = "Select a folder for the SVG files" };
            if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;
            try
            {
                var files = ECPOrderDetailSvgExporter.Export(_viewModel.Rows, dialog.SelectedPath);
                IO.ShowInfo($"Exported {files.Count} SVG file(s) to:\n{dialog.SelectedPath}");
            }
            catch (Exception ex)
            {
                IO.ShowWarning(ex.Message);
            }
        }

        // 添付: writes the order slip (オーダー票) PDF for this zone; extra pages are added when the rows do not fit on one.
        private void _ExportPdf()
        {
            using var dialog = new System.Windows.Forms.SaveFileDialog
            {
                Filter = "PDF (*.pdf)|*.pdf",
                FileName = string.IsNullOrWhiteSpace(_viewModel.Header.OrderNo) ? "order" : _viewModel.Header.OrderNo,
            };
            if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;
            try
            {
                ECPOrderSlipPdfExporter.Export(_viewModel.Header, _viewModel.Rows, dialog.FileName);
                IO.ShowInfo($"Exported PDF to:\n{dialog.FileName}");
            }
            catch (Exception ex)
            {
                IO.ShowWarning(ex.Message);
            }
        }

        private void _Cancel()
        {
            _view.Close();
        }

        // Placeholder content so the screen can be reviewed; replace with data read from the model once the logic is defined.
        private static ECPOrderDetailHeaderModel CreateSampleHeader()
        {
            return new ECPOrderDetailHeaderModel
            {
                PropertyRegNo = "B260000604", OrderNo = "1262702941",
                SalesStaffCode = "19860571", SalesStaffName = "田川 雅浩",
                Status = "納期回答完了",
                DealerCode = "0000027555", DealerName = "住友林業株式会社",
                DealerBranch = "木材建材事業本部大阪営業部 ｿﾘｭｰｼｮﾝｸﾞﾙｰﾌﾟ",
                PropertyName = "株式会社ｺﾍﾞﾙｺﾛｼﾞｽﾃｨｸｽ 加古川地区新事務所建設工事",
                PropertyDetail = "2F②工区-ﾌﾗｯﾄ",
                PostalCode = "675-0137", PrefectureCode = "28", PrefectureName = "兵庫県",
                Address = "加古川市金沢町35番",
                DesignName = "鹿島建設㈱", ContractorCode = "8000000002", ContractorName = "鹿島建設",
                DesiredDate = "2026/10/02",
                UseCode = "01", UseName = "外壁",
                FactoryCode = "320", FactoryName = "市川工場",
                CategoryCode = "105", CategoryName = "ﾌﾗｯﾄﾊﾟﾈﾙ60",
                FaceCode = "02", FaceName = "【表】研磨【裏】普通",
                GasketOn = true, VinylOn = false,
                HardwareCode = "ZZZ", HardwareName = "金物ｵｰﾀﾞｰ作成しない",
                SpecCode = "03", SpecName = "現場塗装",
                DeliveryMethodCode = "01", DeliveryMethodName = "現場納入",
                VehicleCode = "03", VehicleName = "10ﾄﾝ車",
                VehicleTypeCode = "01", VehicleTypeName = "平車",
                LoadWeight = "1000", LoadHeight = "7", MapOn = false,
                ConstructionDrawingNo = "99999",
                ActualCount = "59", ExchangeCount = "398.423",
                BaseArea = "63.300", BaseWeight = "4,003",
                WorkArea = "63.154", WorkWeight = "3,994", TotalLength = "106.545",
            };
        }

        private static List<ECPOrderDetailRowModel> CreateSampleRows()
        {
            const string flat = "ﾌﾗｯﾄﾊﾟﾈﾙ";
            const string ryogaku = "ﾘｮｳｶﾞｸ凸ﾊﾟﾈﾙ";
            var rows = new List<ECPOrderDetailRowModel>
            {
                new() { No = "3", WorkNo = "402", ProductName = flat, PartNumber = "MNH-6060A", Length = "4180", Quantity = "1", QuantityCode = "11", ShapeKind = "FlatCut" },
                new() { No = "4", WorkNo = "403", ProductName = flat, PartNumber = "MNH-6045A", Length = "4180", Quantity = "1", QuantityCode = "09", ShapeKind = "RightBlock", Dimension = "415" },
                new() { No = "5", WorkNo = "404", ProductName = flat, PartNumber = "MNH-6060A", Length = "2210", Quantity = "11" },
                new() { No = "6", WorkNo = "405", ProductName = ryogaku, PartNumber = "MNH-6060T", Length = "2210", Quantity = "1", QuantityCode = "82" },
                new() { No = "7", WorkNo = "406", ProductName = flat, PartNumber = "MNH-6060A", Length = "1425", Quantity = "11" },
                new() { No = "8", WorkNo = "407", ProductName = flat, PartNumber = "MNH-6060A", Length = "1425", Quantity = "1", QuantityCode = "11", ShapeKind = "FlatCut" },
                new() { No = "9", WorkNo = "408", ProductName = ryogaku, PartNumber = "MNH-6060T", Length = "1425", Quantity = "1", QuantityCode = "82" },
                new() { No = "10", WorkNo = "409", ProductName = flat, PartNumber = "MNH-6060A", Length = "1065", Quantity = "4" },
                new() { No = "11", WorkNo = "410", ProductName = flat, PartNumber = "MNH-6060A", Length = "935", Quantity = "4" },
                new() { No = "12", WorkNo = "411", ProductName = flat, PartNumber = "MNH-6060A", Length = "875", Quantity = "11" },
                new() { No = "13", WorkNo = "412", ProductName = ryogaku, PartNumber = "MNH-6060T", Length = "875", Quantity = "1", QuantityCode = "82" },
                new() { No = "14", WorkNo = "413", ProductName = flat, PartNumber = "MNH-6060A", Length = "300", Quantity = "4" },
            };
            for (var i = 0; i < rows.Count; i++)
                rows[i].IsAlternate = i % 2 == 1;
            return rows;
        }
    }
}
