﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Media;
using MySqlX.XDevAPI.Common;
using Newtonsoft.Json;
using OMDb.WinUI3.Models;
using OMDb.WinUI3.Services;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
//using Microsoft.UI.Xaml.Media;
using Microsoft.UI;
using System.Xml.Linq;
using Microsoft.UI.Xaml;
using System.IO;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using OMDb.WinUI3.Services.Settings;

namespace OMDb.WinUI3.ViewModels
{
    public class LabelPropertyViewModel : ObservableObject
    {
        public LabelPropertyViewModel()
        {
            Init();
        }


        private ObservableCollection<LabelPropertyTree> _labelPropertyTrees;
        public ObservableCollection<LabelPropertyTree> LabelPropertyTrees
        {
            get => _labelPropertyTrees;
            set => SetProperty(ref _labelPropertyTrees, value);
        }

        private ObservableCollection<LabelPropertyTree> _lp_Baba=new ObservableCollection<LabelPropertyTree>();
        public ObservableCollection<LabelPropertyTree> LP_Baba
        {
            get => _lp_Baba;
            set => SetProperty(ref _lp_Baba, value);
        }

        //初始化
        private async void Init()
        {
            var lpdbs = await Core.Services.LabelPropertyService.GetAllLabelAsync(DbSelectorService.dbCurrentId);
            if (lpdbs != null)
            {
                Dictionary<string, LabelPropertyTree> dicLpdbs = new Dictionary<string, LabelPropertyTree>();
                var root = lpdbs.Where(p => p.ParentId == null).ToList();
                if (root != null)
                {
                    foreach (var lp_Yeye in root)
                    {
                        dicLpdbs.Add(lp_Yeye.LPId, new LabelPropertyTree(lp_Yeye));
                    }
                }
                foreach (var lp in lpdbs)
                {
                    if (lp.ParentId != null)
                    {
                        if (dicLpdbs.TryGetValue(lp.ParentId, out var parent))
                        {
                            parent.Children.Add(new LabelPropertyTree(lp));
                        }
                    }
                }
                Helpers.WindowHelper.MainWindow.DispatcherQueue.TryEnqueue(() =>
                {
                    LabelPropertyTrees = new ObservableCollection<LabelPropertyTree>();
                    foreach (var item in dicLpdbs)
                    {
                        LabelPropertyTrees.Add(item.Value);
                    }
                });
            }


        }



        public ICommand RefreshCommand => new RelayCommand(() =>
        {
            Init();
            Helpers.InfoHelper.ShowSuccess("刷新完成");
        });
    }
}