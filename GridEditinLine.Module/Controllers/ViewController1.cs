using DevExpress.Blazor;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Blazor.Editors;
using Microsoft.AspNetCore.Components;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace GridEditinLine.Module.Controllers
{
    // For more typical usage scenarios, be sure to check out https://docs.devexpress.com/eXpressAppFramework/DevExpress.ExpressApp.ViewController.
    public partial class ViewController1 : ViewController<ListView>
    {
        private bool isDoubleClick = false;
        private bool singleClickHandled = false;
        private readonly int doubleClickDelay = 300;
        private bool isHandlingClick = false;
        private CancellationTokenSource clickCancellationTokenSource;
        public ViewController1()
        {
            InitializeComponent();
            // Target required Views (via the TargetXXX properties) and create their Actions.
        }
        protected override void OnActivated()
        {
            base.OnActivated();
            // Perform various tasks depending on the target View.
        }
        protected override void OnViewControlsCreated()
        {
            base.OnViewControlsCreated();

            if (View.Editor is DxGridListEditor editor)
            {
                editor.RowClickMode = RowClickMode.SelectOnSingleProcessOnDouble;

                var oldRowClick = editor.GridModel.RowClick;
                var oldRowDoubleClick = editor.GridModel.RowDoubleClick;

                editor.GridModel.RowClick = EventCallback.Factory.Create<GridRowClickEventArgs>(this, async (e) =>
                {
                    clickCancellationTokenSource?.Cancel();
                    clickCancellationTokenSource = new CancellationTokenSource();

                    try
                    {
                        await Task.Delay(doubleClickDelay, clickCancellationTokenSource.Token);

                        if (!isDoubleClick)
                        {
                            await oldRowClick.InvokeAsync(e);

                            var grid = (DxGrid)e.Grid;

                            if (IsAnyColumnModified())
                            {
                                await grid.SaveChangesAsync();
                            }

                            await StartRowEdit(grid, e.VisibleIndex);
                        }
                    }
                    catch (TaskCanceledException)
                    {
                    }
                    finally
                    {
                        isDoubleClick = false;
                    }
                });

                editor.GridModel.RowDoubleClick = EventCallback.Factory.Create<GridRowClickEventArgs>(this, async (e) =>
                {
                    isDoubleClick = true;

                    clickCancellationTokenSource?.Cancel();
                    await oldRowDoubleClick.InvokeAsync(e);

                    var grid = (DxGrid)e.Grid;
                    await grid.SaveChangesAsync();

                    var focusedRowIndex = grid.GetFocusedRowIndex();
                    var dataItem = ResolveObjectFromFocusedRowIndex(focusedRowIndex);

                    if (dataItem != null)
                    {
                        var newObjectSpace = Application.CreateObjectSpace();
                        var objectInNewSpace = newObjectSpace.GetObject(dataItem);
                        var detailView = Application.CreateDetailView(newObjectSpace, objectInNewSpace, true);

                        Frame.SetView(detailView);
                    }
                });
            }
        }

        private object ResolveObjectFromFocusedRowIndex(int focusedRowIndex)
        {
            if (focusedRowIndex < 0) return null;

            var collection = View.ObjectSpace.GetObjects(View.ObjectTypeInfo.Type);
            var enumerableCollection = collection.Cast<object>().ToList();

            if (focusedRowIndex >= 0 && focusedRowIndex < enumerableCollection.Count)
            {
                return enumerableCollection[focusedRowIndex];
            }

            return null;
        }

        private bool IsAnyColumnModified()
        {
            return View.ObjectSpace.IsModified;
        }

        private object GetObjectFromGrid(int visibleIndex)
        {
            var objectSpace = View.ObjectSpace;
            var collection = View.ObjectSpace.GetObjects(View.ObjectTypeInfo.Type);
            var enumerableCollection = collection.Cast<object>().ToList();

            if (enumerableCollection != null && enumerableCollection.Count > visibleIndex)
            {
                return enumerableCollection.ElementAt(visibleIndex);
            }
            else
                return null;
        }

        private async Task StartRowEdit(DxGrid grid, int visibleIndex)
        {
            await grid.StartEditRowAsync(visibleIndex);
        }

        protected override void OnDeactivated()
        {
            // Unsubscribe from previously subscribed events and release other references and resources.
            base.OnDeactivated();
        }
    }
}
