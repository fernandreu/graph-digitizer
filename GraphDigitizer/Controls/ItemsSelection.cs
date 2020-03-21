using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace GraphDigitizer.Controls
{
    /// <summary>
    /// Static class with attached properties to support two-way binding to a ListBox / DataGrid
    /// </summary>
    public static class ItemsSelection
    {
        private static readonly Dictionary<DependencyObject, IList> Subscribers = new Dictionary<DependencyObject, IList>();

        public static readonly DependencyProperty SelectedItemsProperty = DependencyProperty.RegisterAttached(
            "SelectedItems",
            typeof(IList),
            typeof(ItemsSelection),
            new PropertyMetadata(default(IList), OnSelectedItemsChanged));

        public static IList GetSelectedItems(DependencyObject element)
        {
            return (IList)element.GetValue(SelectedItemsProperty);
        }

        public static void SetSelectedItems(DependencyObject element, IList value)
        {
            SetSubscriber(element, value);
            element.SetValue(SelectedItemsProperty, value);
        }

        private static void SetSubscriber(DependencyObject element, IList value)
        {
            if (value == null)
            {
                if (Subscribers.ContainsKey(element))
                {
                    Subscribers.Remove(element);
                }
            }
            else
            {
                Subscribers[element] = value;
            }
        }

        private static void OnSelectedItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is ListBox || d is MultiSelector))
                throw new ArgumentException("This property can only be attached to a ListBox or a MultiSelector (DataGrid)");

            var selector = (Selector)d;
            if (e.OldValue is IList oldList)
            {
                if (oldList is INotifyCollectionChanged obs)
                {
                    obs.CollectionChanged -= OnCollectionChanged;
                }
            }

            if (e.NewValue is IList newList)
            {
                if (newList is INotifyCollectionChanged obs)
                {
                    obs.CollectionChanged += OnCollectionChanged;
                }

                PushCollectionDataToSelectedItems(newList, selector);

                SetSubscriber(d, newList);
            }
            else
            {
                // If we're orphaned, disconnect events.
                selector.SelectionChanged -= OnSelectorSelectionChanged;
                SetSubscriber(d, null);
            }
        }

        private static void PushCollectionDataToSelectedItems(IList obs, DependencyObject selector)
        {
            if (selector is ListBox listBox)
            {
                PushCollectionDataToSelectedItems(listBox, obs, listBox.SelectedItems);
                return;
            }

            if (selector is MultiSelector grid)
            {
                PushCollectionDataToSelectedItems(grid, obs, grid.SelectedItems);
                return;
            }

            throw new ArgumentException("This property can only be attached to a ListBox or a MultiSelector (DataGrid)");
        }

        private static void PushCollectionDataToSelectedItems(Selector selector, IList collectionData, IList selectedItems)
        {
            selector.SelectionChanged -= OnSelectorSelectionChanged;

            selectedItems.Clear();
            foreach (var ob in collectionData)
            {
                selectedItems.Add(ob);
            }

            selector.SelectionChanged += OnSelectorSelectionChanged;
        }

        private static void OnSelectorSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var dep = (DependencyObject)sender;
            var items = GetSelectedItems(dep);
            var col = items as INotifyCollectionChanged;

            // Remove the events so we don't fire back and forth, then re-add them.
            if (col != null)
            {
                col.CollectionChanged -= OnCollectionChanged;
            }

            foreach (var oldItem in e.RemovedItems)
            {
                items.Remove(oldItem);
            }

            foreach (var newItem in e.AddedItems)
            {
                items.Add(newItem);
            }

            if (col != null)
            {
                col.CollectionChanged += OnCollectionChanged;
            }
        }

        private static void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            foreach (var pair in Subscribers)
            {
                if (ReferenceEquals(pair.Value, sender))
                {
                    OnCollectionChanged(pair.Key, e);
                }
            }

        }

        private static void OnCollectionChanged(DependencyObject element, NotifyCollectionChangedEventArgs e)
        {
            // Push the changes to the selected item.
            if (element is ListBox listbox)
            {
                OnCollectionChanged(listbox, e, listbox.SelectedItems);
                return;
            }

            if (element is MultiSelector grid)
            {
                OnCollectionChanged(grid, e, grid.SelectedItems);
                return;
            }
        }

        private static void OnCollectionChanged(Selector selector, NotifyCollectionChangedEventArgs e, IList selectedItems)
        {
            selector.SelectionChanged -= OnSelectorSelectionChanged;

            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                selectedItems.Clear();
            }
            else
            {
                if (e.OldItems != null)
                {
                    foreach (var oldItem in e.OldItems)
                    {
                        selectedItems.Remove(oldItem);
                    }
                }

                if (e.NewItems != null)
                {
                    foreach (var newItem in e.NewItems)
                    {
                        selectedItems.Add(newItem);
                    }
                }
            }

            selector.SelectionChanged += OnSelectorSelectionChanged;
        }
    }
}
