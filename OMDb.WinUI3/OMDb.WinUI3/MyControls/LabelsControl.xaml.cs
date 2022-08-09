﻿using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using OMDb.Core.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Windows.Input;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace OMDb.WinUI3.MyControls
{
    public sealed partial class LabelsControl : UserControl
    {
        public LabelsControl()
        {
            this.InitializeComponent();
        }
        public static readonly DependencyProperty LabelsProperty = DependencyProperty.Register
            (
            "Labels",
            typeof(IEnumerable<Core.DbModels.LabelDb>),
            typeof(UserControl),
            new PropertyMetadata(null, new PropertyChangedCallback(SetLabels))
            );
        private static void SetLabels(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var card = d as LabelsControl;
            if (card != null)
            {
                card.Labels = e.NewValue as IEnumerable<Core.DbModels.LabelDb>;
                if (card.Labels != null)
                {
                    List<Models.Label> list = new List<Models.Label>();
                    foreach (var item in card.Labels)
                    {
                        list.Add(new Models.Label(item) { IsChecked = true });
                    }
                    if(card.Mode == LabelControlMode.Add)
                    {
                        list.Add(new Models.Label(new Core.DbModels.LabelDb() { Name = "+"}) { IsTemp = true,IsChecked = true});
                    }
                    else if(card.Mode == LabelControlMode.Selecte)
                    {
                        list.Insert(0,new Models.Label(new Core.DbModels.LabelDb() { Name = "全选" }) { IsTemp = true, IsChecked = true });
                    }
                    card.GridView_Label.ItemsSource = list;
                    card.LabelsSource = list;
                }
                else
                {
                    card.GridView_Label.ItemsSource = null;
                    card.LabelsSource = null;
                }
            }
        }
        private List<Models.Label> LabelsSource;
        public IEnumerable<Core.DbModels.LabelDb> Labels
        {
            get { return (IEnumerable<Core.DbModels.LabelDb>)GetValue(LabelsProperty); }

            set { SetValue(LabelsProperty, value); }
        }

        public static readonly DependencyProperty ModeProperty = DependencyProperty.Register
            (
            "Mode",
            typeof(LabelControlMode),
            typeof(UserControl),
            new PropertyMetadata(LabelControlMode.None, new PropertyChangedCallback(SetMode))
            );
        private static void SetMode(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
        }
        public LabelControlMode Mode
        {
            get { return (LabelControlMode) GetValue(ModeProperty); }

            set { SetValue(ModeProperty, value); }
        }
        
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var label = (sender as Button).DataContext as Models.Label;
            if(label != null)
            {
                if (Mode == LabelControlMode.Add && label.IsTemp)//新增标签
                {
                    ShowAddLabelsFlyout(sender as Button);
                }
                else if(Mode == LabelControlMode.Selecte && label.IsTemp)//全选、全不选
                {
                    label.IsChecked = !label.IsChecked;
                    LabelsSource.ForEach(p => p.IsChecked = label.IsChecked);
                    CallChanged();
                }
                else
                {
                    CallChanged();
                    switch (Mode)
                    {
                        case LabelControlMode.None: break;
                        case LabelControlMode.Selecte:
                            {
                                label.IsChecked = !label.IsChecked;
                                if(label.IsChecked)
                                {
                                    if(LabelsSource.FirstOrDefault(p=>!p.IsTemp && !p.IsChecked) == null)
                                    {
                                        LabelsSource.First().IsChecked = true;
                                    }
                                }
                                else
                                {
                                    LabelsSource.First().IsChecked = false;
                                }
                            }
                            break;
                        case LabelControlMode.Add: break;
                    }
                }
            }
        }
        private void CallChanged()
        {
            var ls = LabelsSource.Where(p => !p.IsTemp).ToList();
            CheckChanged?.Invoke(ls);
            CheckChangedCommand?.Execute(ls);
        }
        private List<Models.Label> AllLabels;
        private Flyout AddLabelFlyout;
        private async void ShowAddLabelsFlyout(FrameworkElement element)
        {
            if(AllLabels == null)
            {
                var labels = await Core.Services.LabelService.GetAllLabelAsync();
                if (labels != null)
                {
                    AllLabels = labels.Select(p => new Models.Label(p)).ToList();
                    if(labels?.Count!=0)
                    {
                        var dic = Labels.ToDictionary(p => p.Id);
                        foreach(var label in AllLabels)
                        {
                            if(dic.ContainsKey(label.LabelDb.Id))
                            {
                                label.IsChecked = true;
                            }
                        }
                    }
                }
            }
            if(AddLabelFlyout == null)
            {
                AddLabelFlyout = new Flyout();
                AddLabelsControl addLabelsControl = new AddLabelsControl();
                addLabelsControl.Labels = AllLabels.DepthClone<List<Models.Label>>();
                AddLabelFlyout.Content = addLabelsControl;
                addLabelsControl.DoneEvent += AddLabelsControl_DoneEvent;
            }
            AddLabelFlyout.ShowAt(element);
        }

        private void AddLabelsControl_DoneEvent(bool confirm, IEnumerable<Models.Label> labels)
        {
            AddLabelFlyout.Hide();
            if(confirm)
            {
                var dic = labels.ToDictionary(p => p.LabelDb.Id);
                foreach (var label in AllLabels)
                {
                    if(dic.TryGetValue(label.LabelDb.Id, out var value))
                    {
                        label.IsChecked = value.IsChecked;
                    }
                }
                Labels = labels.Where(p=>p.IsChecked).Select(p => p.LabelDb).ToList();
            }
            else
            {
                (AddLabelFlyout.Content as AddLabelsControl).Labels = AllLabels.DepthClone<List<Models.Label>>();
            }
        }

        /// <summary>
        /// 选择标签委托
        /// </summary>
        /// <param name="labels">所有标签</param>
        /// <param name="label">当前触发标签</param>
        public delegate void CheckChangedEventHandel(IEnumerable<Models.Label> allLabels);
        private CheckChangedEventHandel CheckChanged;

        public static readonly DependencyProperty CheckChangedCommandProperty
           = DependencyProperty.Register(
               nameof(CheckChangedCommand),
               typeof(string),
               typeof(LabelsControl),
               new PropertyMetadata(string.Empty));

        public ICommand CheckChangedCommand
        {
            get => (ICommand)GetValue(CheckChangedCommandProperty);
            set => SetValue(CheckChangedCommandProperty, value);
        }

        /// <summary>
        /// 选择标签后触发
        /// </summary>
        public event CheckChangedEventHandel OnCheckChanged
        {
            add
            {
                CheckChanged += value;
            }
            remove
            {
                CheckChanged -= value;
            }
        }

        public enum LabelControlMode
        {
            [Description("无")]
            None,
            [Description("多个选择模式")]
            Selecte,
            [Description("添加模式")]
            Add
        }
    }
    public sealed class CheckToOpacityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if ((bool)value)
            {
                return 1.0;
            }
            else
            {
                return 0.4;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
