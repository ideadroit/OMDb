﻿using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Newtonsoft.Json;
using OMDb.WinUI3.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace OMDb.WinUI3.ViewModels
{
    public class EntryViewModel : ObservableObject
    {
        public ObservableCollection<EnrtyStorage> EnrtyStorages { get; set; } = Services.ConfigService.EnrtyStorages;
        private List<Core.Models.Entry> entries;
        public List<Core.Models.Entry> Entries
        {
            get => entries;
            set
            {
                SetProperty(ref entries, value);
            }
        }
        public EntryViewModel()
        {
            Init();
        }
        private async void Init()
        {
            var queryResults = await Core.Services.EntryService.QueryEntryAsync(Core.Enums.SortType.LastUpdateTime, Core.Enums.SortWay.Positive);
            if(queryResults?.Count > 0)
            {
                Entries = await Core.Services.EntryService.QueryEntryAsync(queryResults.Select(p=>p.ToQueryItem()).ToList());
            }
        }
        public ICommand RefreshCommand => new RelayCommand(() =>
        {
            Init();
            Helpers.InfoHelper.ShowMsg("刷新完成");
        });
        public ICommand ItemClickCommand => new RelayCommand<Core.Models.Entry>(async(entry) =>
        {
            await Services.EntryService.EditEntryAsync(entry);
        });
    }
}
