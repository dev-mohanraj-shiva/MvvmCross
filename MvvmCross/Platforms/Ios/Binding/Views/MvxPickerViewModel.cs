﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MS-PL license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Specialized;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using MvvmCross.Binding.Attributes;
using MvvmCross.Binding.Extensions;
using MvvmCross.Logging;
using MvvmCross.WeakSubscription;
using UIKit;

namespace MvvmCross.Platforms.Ios.Binding.Views
{
    public class MvxPickerViewModel
        : UIPickerViewModel
    {
        private readonly UIPickerView _pickerView;
        private IEnumerable _itemsSource;
        private IDisposable _subscription;
        private object _selectedItem;

        public bool ReloadOnAllItemsSourceSets { get; set; }

        public MvxPickerViewModel(UIPickerView pickerView)
        {
            _pickerView = pickerView;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_subscription != null)
                {
                    _subscription.Dispose();
                    _subscription = null;
                }
            }

            base.Dispose(disposing);
        }

        [MvxSetToNullAfterBinding]
        public virtual IEnumerable ItemsSource
        {
            get
            {
                return _itemsSource;
            }
            set
            {
                if (ReferenceEquals(_itemsSource, value)
                    && !ReloadOnAllItemsSourceSets)
                    return;

                if (_subscription != null)
                {
                    _subscription.Dispose();
                    _subscription = null;
                }

                _itemsSource = value;

                var collectionChanged = _itemsSource as INotifyCollectionChanged;
                if (collectionChanged != null)
                {
                    _subscription = collectionChanged.WeakSubscribe(CollectionChangedOnCollectionChanged);
                }

                Reload();
                ShowSelectedItem();
            }
        }

        protected virtual void CollectionChangedOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            MvxLogHost.Default?.LogTrace(
                "CollectionChanged called inside MvxPickerViewModel - beware that this isn't fully tested - picker might not fully support changes while the picker is visible");
            Reload();
        }

        protected virtual void Reload()
        {
            _pickerView.ReloadComponent(0);
        }

        public override nint GetComponentCount(UIPickerView picker)
        {
            return 1;
        }

        public override nint GetRowsInComponent(UIPickerView picker, nint component)
        {
            return _itemsSource?.Count() ?? 0;
        }

        public override string GetTitle(UIPickerView picker, nint row, nint component)
        {
            return _itemsSource == null ? "-" : RowTitle(row, _itemsSource.ElementAt((int)row));
        }

        protected virtual string RowTitle(nint row, object item)
        {
            return item.ToString();
        }

        public override void Selected(UIPickerView picker, nint row, nint component)
        {
            if (_itemsSource.Count() == 0)
                return;

            _selectedItem = _itemsSource.ElementAt((int)row);

            var handler = SelectedItemChanged;
            handler?.Invoke(this, EventArgs.Empty);

            var command = SelectedChangedCommand;
            if (command != null)
                if (command.CanExecute(_selectedItem))
                    command.Execute(_selectedItem);
        }

        public object SelectedItem
        {
            get
            {
                return _selectedItem;
            }
            set
            {
                _selectedItem = value;
                ShowSelectedItem();
            }
        }

        public event EventHandler SelectedItemChanged;

        public ICommand SelectedChangedCommand { get; set; }

        protected virtual void ShowSelectedItem()
        {
            if (_itemsSource == null)
                return;

            var position = _itemsSource.GetPosition(_selectedItem);
            if (position < 0)
                return;

            var animated = !_pickerView.Hidden;
            _pickerView.Select(position, 0, animated);
        }
    }
}
