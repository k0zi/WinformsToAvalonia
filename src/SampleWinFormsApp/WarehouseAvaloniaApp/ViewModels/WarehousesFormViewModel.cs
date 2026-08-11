using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WarehouseApp.Common;
using WarehouseApp.Data.Models;

namespace WarehouseAvaloniaApp.ViewModels;

/// <summary>
/// ViewModel for WarehousesForm (user customizations).
/// This file is preserved during reconversion - add your custom code here.
/// </summary>
public partial class WarehousesFormViewModel : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    internal async Task LoadTreeAsync()
        {
            var (warehouses, locations) = await Task.Run(() =>
            {
                using var ctx = Db.CreateContext();
                return (ctx.Warehouses.ToList(), ctx.Locations.ToList());
            });
    
            locationsTreeView.Nodes.Clear();
            foreach (var warehouse in warehouses.OrderBy(w => w.Name))
            {
                var whNode = new TreeNode(warehouse.Name) { Tag = warehouse, ImageKey = "Warehouse", SelectedImageKey = "Warehouse" };
                foreach (var zone in locations.Where(l => l.WarehouseId == warehouse.Id && l.LocationType == LocationType.Zone).OrderBy(l => l.Name))
                {
                    var zoneNode = new TreeNode(zone.Name) { Tag = zone, ImageKey = "Zone", SelectedImageKey = "Zone" };
                    foreach (var shelf in locations.Where(l => l.ParentLocationId == zone.Id).OrderBy(l => l.Name))
                    {
                        zoneNode.Nodes.Add(new TreeNode(shelf.Name) { Tag = shelf, ImageKey = "Shelf", SelectedImageKey = "Shelf" });
                    }
                    whNode.Nodes.Add(zoneNode);
                }
                locationsTreeView.Nodes.Add(whNode);
            }
            locationsTreeView.ExpandAll();
            recordCountLabel.Text = $"{warehouses.Count} warehouse(s), {locations.Count} location(s)";
            ShelfContents.Clear();
            SelectedName = "Select a warehouse or location";
            CapacityGauge = 0;
            CapacityDetail = string.Empty;
        }

    internal async Task ShowWarehouseAsync(Warehouse warehouse)
        {
            SelectedName = $"{warehouse.Name} ({warehouse.Code})";
            var onHand = await Task.Run(() =>
            {
                using var ctx = Db.CreateContext();
                return ctx.StockLevels.Where(s => s.WarehouseId == warehouse.Id).Sum(s => (int?)s.QuantityOnHand) ?? 0;
            });
            var percent = warehouse.CapacityUnits == 0 ? 0 : Math.Min(100.0, onHand * 100.0 / warehouse.CapacityUnits);
            CapacityGauge = percent;
            CapacityDetail = $"{onHand} / {warehouse.CapacityUnits} units";
            await LoadShelfContentsAsync(null);
        }

    internal async Task ShowLocationAsync(Location location)
        {
            SelectedName = $"{location.Name} ({location.LocationType})";
            var onHand = await Task.Run(() =>
            {
                using var ctx = Db.CreateContext();
                return ctx.StockLevels.Where(s => s.LocationId == location.Id).Sum(s => (int?)s.QuantityOnHand) ?? 0;
            });
            var percent = location.CapacityUnits == 0 ? 0 : Math.Min(100.0, onHand * 100.0 / location.CapacityUnits);
            CapacityGauge = percent;
            CapacityDetail = $"{onHand} / {location.CapacityUnits} units";
            await LoadShelfContentsAsync(location.Id);
        }

    internal async Task LoadShelfContentsAsync(int? locationId)
        {
            ShelfContents.Clear();
            if (locationId is null)
            {
                return;
            }
    
            var stockLevels = await Task.Run(() =>
            {
                using var ctx = Db.CreateContext();
                return ctx.StockLevels.Include(s => s.Product).Where(s => s.LocationId == locationId).ToList();
            });
    
            foreach (var stock in stockLevels)
            {
                var item = new ListViewItem(stock.Product.Name);
                item.SubItems.Add(stock.QuantityOnHand.ToString());
                item.SubItems.Add(stock.QuantityReserved.ToString());
                ShelfContents.Add(item);
            }
        }

    internal async void AddWarehouse()
        {
            var name = InputBoxHelper.Show(this, "New Warehouse", "Warehouse name:");
            if (string.IsNullOrWhiteSpace(name))
            {
                return;
            }
    
            using var ctx = Db.CreateContext();
            var code = $"WH-{name[..Math.Min(3, name.Length)].ToUpperInvariant()}";
            ctx.Warehouses.Add(new Warehouse { Name = name, Code = code, CapacityUnits = 5000 });
            await ctx.SaveChangesAsync();
            await LoadTreeAsync();
        }

    internal async void AddZone()
        {
            if (Locations?.Tag is not Warehouse warehouse)
            {
                await WarehouseAvaloniaApp.Common.Dialogs.ShowAsync("Select a warehouse first.","New Zone",WarehouseAvaloniaApp.Common.MessageBoxButtons.OK,WarehouseAvaloniaApp.Common.MessageBoxIcon.Information);
                return;
            }
    
            var name = InputBoxHelper.Show(this, "New Zone", "Zone name:");
            if (string.IsNullOrWhiteSpace(name))
            {
                return;
            }
    
            using var ctx = Db.CreateContext();
            ctx.Locations.Add(new Location
            {
                WarehouseId = warehouse.Id,
                Code = $"{warehouse.Code}-{name}",
                Name = name,
                LocationType = LocationType.Zone,
                CapacityUnits = warehouse.CapacityUnits / 4
            });
            await ctx.SaveChangesAsync();
            await LoadTreeAsync();
        }

    internal async void AddShelf()
        {
            if (Locations?.Tag is not Location zone || zone.LocationType != LocationType.Zone)
            {
                await WarehouseAvaloniaApp.Common.Dialogs.ShowAsync("Select a zone first.","New Shelf",WarehouseAvaloniaApp.Common.MessageBoxButtons.OK,WarehouseAvaloniaApp.Common.MessageBoxIcon.Information);
                return;
            }
    
            var name = InputBoxHelper.Show(this, "New Shelf", "Shelf name:");
            if (string.IsNullOrWhiteSpace(name))
            {
                return;
            }
    
            using var ctx = Db.CreateContext();
            ctx.Locations.Add(new Location
            {
                WarehouseId = zone.WarehouseId,
                ParentLocationId = zone.Id,
                Code = $"{zone.Code}-{name}",
                Name = name,
                LocationType = LocationType.Shelf,
                CapacityUnits = zone.CapacityUnits / 2
            });
            await ctx.SaveChangesAsync();
            await LoadTreeAsync();
        }

    internal async Task DeleteSelectedNodeAsync()
        {
            var tag = Locations?.Tag;
            if (tag is null)
            {
                return;
            }
    
            var confirm = MessageBox.Show(this, "Delete the selected node?", "Confirm Delete",
                WarehouseAvaloniaApp.Common.MessageBoxButtons.YesNo, WarehouseAvaloniaApp.Common.MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
            if (confirm != WarehouseAvaloniaApp.Common.DialogResult.Yes)
            {
                return;
            }
    
            try
            {
                using var ctx = Db.CreateContext();
                switch (tag)
                {
                    case Warehouse warehouse:
                        var trackedWh = await ctx.Warehouses.FindAsync(warehouse.Id);
                        if (trackedWh is not null)
                        {
                            ctx.Warehouses.Remove(trackedWh);
                        }
                        break;
                    case Location location:
                        var trackedLoc = await ctx.Locations.FindAsync(location.Id);
                        if (trackedLoc is not null)
                        {
                            ctx.Locations.Remove(trackedLoc);
                        }
                        break;
                }
                await ctx.SaveChangesAsync();
                await LoadTreeAsync();
            }
            catch (Exception ex)
            {
                await WarehouseAvaloniaApp.Common.Dialogs.ShowAsync($"Could not delete — it may still contain sub-locations or stock.\n\n{ex.Message}",                "Delete Failed",WarehouseAvaloniaApp.Common.MessageBoxButtons.OK,WarehouseAvaloniaApp.Common.MessageBoxIcon.Error);
            }
        }

}
