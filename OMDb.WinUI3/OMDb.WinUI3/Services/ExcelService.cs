﻿using ICSharpCode.SharpZipLib.Core;
using NPOI.POIFS.FileSystem;
using NPOI.SS.Formula;
using OMDb.Core.DbModels;
using OMDb.Core.Enums;
using OMDb.Core.Models;
using OMDb.WinUI3.Events;
using OMDb.WinUI3.Helpers;
using OMDb.WinUI3.Models;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OMDb.WinUI3.Services
{
    public static class ExcelService
    {
        public static void ExportExcel(string filePath, Models.EnrtyStorage enrtyStorage)
        {
            if (string.IsNullOrEmpty(filePath)) return;

            //用于支持gb2312         
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            //創建數據表            
            DataTable dataTable = new DataTable();
            //基本信息列
            dataTable.Columns.Add("Name", typeof(string));
            dataTable.Columns.Add("ReleaseDate", typeof(string));
            dataTable.Columns.Add("MyRating", typeof(double));

            //詞條屬性列
            var result_LabelInfo = Core.Services.LabelService.GetAllLabel(Settings.DbSelectorService.dbCurrentId);
            var label_Property = result_LabelInfo.Where(a => a.IsProperty).ToList();
            var label_Classification = result_LabelInfo.Where(a => !a.IsProperty).Where(c => (!label_Property.Select(b => b.Id).Contains(c.ParentId))).ToList();
            label_Property.ForEach(s => dataTable.Columns.Add(s.Name, typeof(string)));
            //分類列
            dataTable.Columns.Add("Classification", typeof(string));

            dataTable.Columns.Add("SaveType", typeof(string));
            dataTable.Columns.Add("path_source", typeof(string));
            dataTable.Columns.Add("path_entry", typeof(string));
            dataTable.Columns.Add("path_cover", typeof(string));


            var result_EntryLabel = Core.Services.EntryLabelService.SelectAllEntryLabel(enrtyStorage.StorageName);


            var result_EntryInfo = Core.Services.EntryCommonService.SelectEntry(enrtyStorage.StorageName);
            foreach (var item in result_EntryInfo)
            {
                var data = (IDictionary<String, Object>)item;
                object eid, name, releaseData, myRating, path_cover, path_source, path_entry, saveType;
                data.TryGetValue("Eid", out eid);
                data.TryGetValue("NameStr", out name);
                data.TryGetValue("ReleaseDate", out releaseData);
                data.TryGetValue("MyRating", out myRating);


                data.TryGetValue("SaveType", out saveType);
                data.TryGetValue("path_entry", out path_entry);
                data.TryGetValue("path_cover", out path_cover);
                data.TryGetValue("path_source", out path_source);


                //创建数据行
                DataRow row = dataTable.NewRow();
                //基本
                row["Name"] = name;
                row["ReleaseDate"] = releaseData;
                row["MyRating"] = myRating;

                //屬性
                var el = result_EntryLabel.Where(a => a.EntryId == Convert.ToString(eid));
                foreach (var lp in label_Property)
                {
                    var label_Property_Child = result_LabelInfo.Where(a => a.ParentId == lp.Id);
                    foreach (var lpc in label_Property_Child)
                    {
                        if (el.Select(a => a.LabelId).Contains(lpc.Id))
                        {
                            if (row[lp.Name].ToString().Length > 0)
                                row[lp.Name] += "/";
                            row[lp.Name] += lpc.Name;
                        }
                    }
                }
                //分類
                foreach (var lc in label_Classification)
                {
                    if (el.Select(a => a.LabelId).Contains(lc.Id))
                    {
                        if (row["Classification"].ToString().Length > 0) row["Classification"] += "/";
                        row["Classification"] += lc.Name;
                        break;
                    }
                }


                row["path_entry"] = System.IO.Path.Combine(enrtyStorage.StoragePath, ConfigService.OMDbFolder, Settings.DbSelectorService.dbCurrentName, Convert.ToString(path_entry));
                row["path_cover"] = System.IO.Path.Combine(enrtyStorage.StoragePath, ConfigService.OMDbFolder, Settings.DbSelectorService.dbCurrentName, Convert.ToString(path_entry), Convert.ToString(path_cover));

                var saveMode = Convert.ToInt16(saveType) == 1 ? SaveType.Folder : Convert.ToInt16(saveType) == 2 ? SaveType.Files : SaveType.Local;
                row["SaveType"] = saveMode;
                switch (saveMode)
                {
                    case SaveType.Folder:
                        row["path_source"] = enrtyStorage.StoragePath + path_source;
                        break;
                    case SaveType.Files:
                        var lstPath = Convert.ToString(path_source).Split(">.<").ToList();
                        var lstPath_Full = new List<string>();
                        foreach (var path in lstPath)
                        {
                            lstPath_Full.Add(enrtyStorage.StoragePath + path);
                        }
                        row["path_source"] = string.Format("<{0}>", string.Join(">,<", lstPath_Full));
                        break;
                    case SaveType.Local:
                        row["path_source"] = string.Empty;
                        break;
                    default:
                        break;
                }
                dataTable.Rows.Add(row);
            }



            //DataTable的列名和excel的列名对应字典，因为excel的列名一般是中文的，DataTable的列名是英文的，字典主要是存储excel和DataTable列明的对应关系，当然我们也可以把这个对应关系存在配置文件或者其他地方
            Dictionary<string, string> dir = new Dictionary<string, string>();
            dir.Add("Name", "詞條名稱");
            dir.Add("ReleaseDate", "發行日期");
            dir.Add("MyRating", "評分");

            label_Property.ForEach(s => dir.Add(s.Name, s.Name));
            dir.Add("Classification", "分類");

            dir.Add("SaveType", "存儲模式");
            dir.Add("path_source", "存儲地址");
            dir.Add("path_entry", "詞條地址");
            dir.Add("path_cover", "封面地址");
            //使用helper类导出DataTable数据到excel表格中,参数依次是 （DataTable数据源;  excel表名;  excel存放位置的绝对路径; 列名对应字典; 是否清空以前的数据，设置为false，表示内容追加; 每个sheet放的数据条数,如果超过该条数就会新建一个sheet存储）
            ExcelHelper.ExportDTtoExcel(dataTable, "Info表", filePath, dir, true);

        }


        //从Excel中导入数据到界面
        public static async void ImportExcel(string filePath, Models.EnrtyStorage enrtyStorage)
        {
            if (string.IsNullOrEmpty(filePath)) return;
            //用于支持gb2312    
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            //DataTable的列名和excel的列名对应字典

            //读取数据到DataTable，参数依此是（excel文件路径，列名所在行，sheet索引）
            DataTable dt = ExcelHelper.ImportExceltoDt(filePath, 1, 0);
            //遍历DataTable---------------------------

            //基本信息
            List<string> list = new List<string>() { "詞條名稱", "發行日期", "評分", "封面地址", "分類" };
            //已有屬性
            var result_LabelInfo = Core.Services.LabelService.GetAllLabel(Settings.DbSelectorService.dbCurrentId);
            var label_Property_Parent = result_LabelInfo.Where(a => a.IsProperty);

            foreach (DataColumn item in dt.Columns)
            {
                if (!list.Contains(item.ColumnName) && !label_Property_Parent.Select(a => a.Name).Contains(item.ColumnName))
                {
                    var ldb = new LabelDb()
                    {
                        Name = item.ColumnName,
                        DbSourceId = Settings.DbSelectorService.dbCurrentId,
                        IsProperty = true,
                        IsShow = false,
                    };
                    Core.Services.LabelService.AddLabel(ldb);
                }
            }

            //重新加载已有属性
            result_LabelInfo = Core.Services.LabelService.GetAllLabel(Settings.DbSelectorService.dbCurrentId);
            label_Property_Parent = result_LabelInfo.Where(a => a.IsProperty);

            //遍历DataTable中的数据并插入数据库
            foreach (DataRow row in dt.Rows)
            {
                try
                {
                    foreach (DataColumn item in dt.Columns)
                    {
                        var eid = Guid.NewGuid().ToString();
                        EntryDb edb = new EntryDb() { EntryId = eid };
                        List<EntryNameDb> endbs = new List<EntryNameDb>();
                        List<EntrySourceDb> esdbs = new List<EntrySourceDb>();

                        SaveType saveMode = SaveType.Local;
                        var strPath = string.Empty;
                        int n = 0;

                        var baseInfo = list.Where(a => a.Equals(item.ColumnName));
                        if (baseInfo.Count() > 0)
                        {
                            switch (baseInfo.FirstOrDefault())
                            {
                                case "詞條名稱":
                                    var strName = Convert.ToString(row[n]);
                                    var lstName = strName.Split('/').ToList();//根据分隔符构建list
                                    int c = 0;
                                    foreach (var name in lstName)
                                    {
                                        endbs.Add(new EntryNameDb()
                                        {
                                            Name = name,
                                            EntryId = eid,
                                            IsDefault = c == 0 ? true : false,
                                        });
                                    }
                                    break;
                                case "發行日期":
                                    edb.ReleaseDate = Convert.ToDateTime(row[n]);
                                    break;
                                case "評分":
                                    edb.MyRating = Convert.ToDouble(row[n]);
                                    break;
                                case "封面地址":
                                    edb.CoverImg = Convert.ToString(row[n]);
                                    break;
                                case "存储模式":
                                    try { saveMode = (SaveType)Enum.Parse(typeof(SaveType), Convert.ToString(row[n])); } catch { }
                                    break;
                                case "存儲地址"://绝对地址
                                    strPath = Convert.ToString(row[n]);
                                    break;
                                case "分類":
                                    //暂不处理
                                    break;
                                default:
                                    break;
                            }
                        }
                        //标签->属性 插入
                        var propertyInfo = label_Property_Parent.Where(a => a.Name.Equals(item.ColumnName));
                        if (propertyInfo.Count() > 0)
                        {
                            var label_Property_Childs = result_LabelInfo.Where(a => a.ParentId == propertyInfo.FirstOrDefault().Id);
                            var property_Child = Convert.ToString(row[n]);
                            //不存在 属性_儿子
                            if (!label_Property_Childs.Select(a => a.Name).Contains(property_Child))
                            {
                                var ldb = new LabelDb()
                                {
                                    Id = Guid.NewGuid().ToString(),
                                    Name = property_Child,
                                    DbSourceId = Settings.DbSelectorService.dbCurrentId,
                                    IsProperty = false,
                                    IsShow = false,
                                    ParentId = propertyInfo.FirstOrDefault().Id,
                                };
                                result_LabelInfo.Add(ldb);
                                Core.Services.LabelService.AddLabel(ldb);
                                var eldb = new EntryLabelDb()
                                {
                                    DbId = enrtyStorage.StorageName,
                                    LabelId = ldb.Id,
                                    EntryId = eid,
                                };
                                Core.Services.EntryLabelService.AddEntryLabel(eldb);

                            }
                            //已存在 属性_儿子
                            else
                            {
                                var labelId = label_Property_Childs.Where(a => a.Name.Equals(property_Child)).FirstOrDefault().Id;
                                var eldb = new EntryLabelDb()
                                {
                                    DbId = enrtyStorage.StorageName,
                                    LabelId = labelId,
                                    EntryId = eid,
                                };
                                Core.Services.EntryLabelService.AddEntryLabel(eldb);
                            }
                        }

                        //校验存储地址是否合法
                        switch (saveMode)
                        {
                            case SaveType.Folder:
                                edb.SaveType = '1';
                                if (!strPath.StartsWith(enrtyStorage.StorageName, StringComparison.OrdinalIgnoreCase))
                                    //logInfo
                                    break;
                                var esdb = new EntrySourceDb()
                                {
                                    EntryId = eid,
                                    FileType = '1',
                                    Id = Guid.NewGuid().ToString(),
                                    Path = strPath,
                                };
                                esdbs.Add(esdb);
                                break;
                            case SaveType.Files:
                                edb.SaveType = '2';
                                var lstPath = strPath.Substring(1, strPath.Length - 2).Split(">,<").ToList();
                                foreach (var path in lstPath)
                                {
                                    if (!strPath.StartsWith(enrtyStorage.StorageName, StringComparison.OrdinalIgnoreCase)) continue;
                                    var esdb_s = new EntrySourceDb()
                                    {
                                        EntryId = eid,
                                        FileType = stringEx.GetFileType(path),
                                        Id = Guid.NewGuid().ToString(),
                                        Path = path,
                                    };
                                    esdbs.Add(esdb_s);
                                }
                                break;
                            case SaveType.Local:
                                edb.SaveType = '3';
                                break;
                            default:
                                break;
                        }

                        //创建词条路径
                        if (!edb.Path.Contains(System.IO.Path.Combine(enrtyStorage.StorageName, ConfigService.OMDbFolder, Settings.DbSelectorService.dbCurrentName)))//不在仓库路径内，强设置词条路径
                            edb.Path = System.IO.Path.Combine(enrtyStorage.StoragePath, ConfigService.OMDbFolder, endbs[0].Name);
                        if (Directory.Exists(edb.Path))
                        {
                            int i = 1;
                            while (true)
                            {
                                string newPath = $"{edb.Path}({i++})";
                                if (!Directory.Exists(newPath))
                                {
                                    edb.Path = newPath;
                                    break;
                                }
                            }
                        }//重名路径 -> 改名
                        //创建词条文件夹
                        Directory.CreateDirectory(edb.Path);
                        Directory.CreateDirectory(Path.Combine(edb.Path, Services.ConfigService.AudioFolder));
                        Directory.CreateDirectory(Path.Combine(edb.Path, Services.ConfigService.ImgFolder));
                        Directory.CreateDirectory(Path.Combine(edb.Path, Services.ConfigService.VideoFolder));
                        Directory.CreateDirectory(Path.Combine(edb.Path, Services.ConfigService.ResourceFolder));
                        Directory.CreateDirectory(Path.Combine(edb.Path, Services.ConfigService.SubFolder));
                        Directory.CreateDirectory(Path.Combine(edb.Path, Services.ConfigService.InfoFolder));
                        Directory.CreateDirectory(Path.Combine(edb.Path, Services.ConfigService.MoreFolder));



                        //复制封面图(Cover)、并同步修改封面路径
                        if (!System.IO.Directory.Exists(edb.CoverImg) || !stringEx.GetFileType(edb.CoverImg).Equals('1'))//不存在该路径或该文件不为图片
                        {
                            switch (saveMode)
                            {
                                case SaveType.Folder:
                                    //封面空 -> 尋找指定文件夾中圖片 -> 尋找指定文件夾中視頻縮略圖 -> 設置默認封面
                                    var files_1 = Helpers.FileHelper.FindExplorerItems(strPath).FirstOrDefault().Children;
                                    files_1.Where(a => stringEx.GetFileType(a.FullName) == '1').FirstOrDefault();

                                    break;
                                case SaveType.Files:
                                    break;
                                case SaveType.Local:
                                    break;
                                default:
                                    break;
                            }

                        }
                        var coverType = Path.GetFileName(edb.CoverImg).SubString_A21(".", 1, false);
                        //數據庫 詞條路徑&圖片路徑 取相對地址
                        /*entryDetail.Entry.Path = Helpers.PathHelper.EntryRelativePath(entryDetail.Entry);
                        
                        
                        string newImgCoverPath = Path.Combine(entryDetail.FullEntryPath, Services.ConfigService.InfoFolder, "Cover" + coverType);
                        if (newImgCoverPath != entryDetail.FullCoverImgPath) { File.Copy(entryDetail.FullCoverImgPath, newImgCoverPath, true); }
                        entryDetail.FullCoverImgPath = newImgCoverPath;

                        //创建元数据(MataData)
                        InitFile(entryDetail);

                        //保存至数据库
                        await SaveToDbAsync(entryDetail);

                        //这时已经是相对路径
                        Helpers.InfoHelper.ShowSuccess("创建成功");
                        GlobalEvent.NotifyAddEntry(null, new EntryEventArgs(entryDetail.Entry));
                        return entryDetail.Entry.EntryId;*/



                        Core.Services.EntryService.AddEntry(edb, enrtyStorage.StorageName);
                        Core.Services.EntryNameSerivce.AddEntryName(endbs, enrtyStorage.StorageName);
                        Core.Services.EntrySourceSerivce.AddEntrySource(esdbs, enrtyStorage.StorageName);
                    }
                }
                catch (Exception ex)
                {

                    throw ex;
                }
            }

            /*//显示数据
            ShowInfo(tmpInfo);

            //导入成功提示
            ImportSuccessTips(filePathAndName);*/

        }
    }
}
