using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using ACTrigger.UI.ViewModels;
using ACTrigger.UI.Views;
using ACTrigger.Core.Services;
using System;

namespace ACTrigger.UI;

public partial class App : Application
{
    
    
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var settingsService =
                new SettingsService();

            var settings =
                settingsService.Load();

            var vm =
                new MainWindowViewModel();

            

            desktop.MainWindow =
                new MainWindow
                {
                    DataContext = vm
                };

            //desktop.MainWindow.Closed += (_, _) =>
            //{
            //    System.Environment.Exit(0);
            //};

            var damageOut =
                new DamageOutWindow
                {
                    DataContext = vm
                };

            var damageIn =
                new DamageInWindow
                {
                    DataContext = vm
                };

            var debuffWindow =
                new DebuffWindow
                {
                    DataContext = vm
                };

            desktop.MainWindow.Closed += (_, _) =>
            {
                damageOut.Close();
                damageIn.Close();
                debuffWindow.Close();
            };

            damageOut.Position =
                new PixelPoint(
                    (int)settings.DamageOut.X,
                    (int)settings.DamageOut.Y);

            damageIn.Position =
                new PixelPoint(
                    (int)settings.DamageIn.X,
                    (int)settings.DamageIn.Y);
            debuffWindow.Position =
                new PixelPoint(
                    (int)settings.DebuffOverlay.X,
                    (int)settings.DebuffOverlay.Y);
                        

            damageOut.PositionChanged += (_, _) =>
            {
                var current = settingsService.Load();
                settings.DamageOut.X =
                    damageOut.Position.X;

                settings.DamageOut.Y =
                    damageOut.Position.Y;

                settingsService.Save(current);
            };

            damageIn.PositionChanged += (_, _) =>
            {
                var current = settingsService.Load();
                settings.DamageIn.X =
                    damageIn.Position.X;

                settings.DamageIn.Y =
                    damageIn.Position.Y;

                settingsService.Save(current);
            };

            debuffWindow.PositionChanged += (_, _) =>
            {
                var current = settingsService.Load();
                settings.DebuffOverlay.X =
                    debuffWindow.Position.X;

                settings.DebuffOverlay.Y =
                    debuffWindow.Position.Y;

                settingsService.Save(current);
            };

            vm.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName ==
                    nameof(MainWindowViewModel.OverlayEnabled))
                {
                    damageOut.Opacity =
                        vm.OverlayEnabled &&
                        vm.ShowDamageOut
                            ? 1
                            : 0;

                    damageIn.Opacity =
                        vm.OverlayEnabled &&
                        vm.ShowDamageIn
                            ? 1
                            : 0;
                    
                    debuffWindow.Opacity =
                        vm.OverlayEnabled &&
                        vm.ShowDebuffs
                            ? 1
                            : 0;
                }

                if (e.PropertyName ==
                    nameof(MainWindowViewModel.ShowDamageOut))
                {
                    damageOut.Opacity =
                        vm.OverlayEnabled &&
                        vm.ShowDamageOut
                            ? 1
                            : 0;
                }

                if (e.PropertyName ==
                    nameof(MainWindowViewModel.ShowDamageIn))
                {
                    damageIn.Opacity =
                        vm.OverlayEnabled &&
                        vm.ShowDamageIn
                            ? 1
                            : 0;
                }
                if (e.PropertyName ==
                    nameof(MainWindowViewModel.ShowDebuffs))
                {
                    debuffWindow.Opacity =
                        vm.OverlayEnabled &&
                        vm.ShowDebuffs
                            ? 1
                            : 0;
                }
                if (e.PropertyName ==
                    nameof(MainWindowViewModel.EditOverlayLayout))
                {
                    damageOut.SetLayoutMode(
                        vm.EditOverlayLayout);

                    damageIn.SetLayoutMode(
                        vm.EditOverlayLayout);

                    debuffWindow.SetLayoutMode(
                        vm.EditOverlayLayout);
                
                }
                if (e.PropertyName ==
                    nameof(MainWindowViewModel.AllowTargeting))
                {
                    debuffWindow.SetLayoutMode(
                        vm.EditOverlayLayout);
                }
                if (e.PropertyName ==
                    nameof(MainWindowViewModel.SessionActive))
                {
                    debuffWindow.SetLayoutMode(
                        vm.EditOverlayLayout);
                }
            }; 

            damageOut.Show();
            damageIn.Show();
            debuffWindow.Show();

            //applying these settings after show necessary for
            //clickthrough to be applied on Windows
            //dispatcher used to delay settings until mapping complete

            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                damageOut.SetLayoutMode(vm.EditOverlayLayout);
                damageIn.SetLayoutMode(vm.EditOverlayLayout);
                //debuffWindow.SetLayoutMode(vm.EditOverlayLayout);
            }, Avalonia.Threading.DispatcherPriority.Loaded);

            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                debuffWindow.SetLayoutMode(vm.EditOverlayLayout);
            }, Avalonia.Threading.DispatcherPriority.Background);


            // NOTE:
            // Using Opacity instead of Show()/Hide().
            // Avalonia window state becomes corrupted after
            // repeated Show()/Hide() cycles on X11 (or everywhere).
            
            damageOut.Opacity =
                vm.OverlayEnabled &&
                vm.ShowDamageOut
                    ? 1
                    : 0;

            damageIn.Opacity =
                vm.OverlayEnabled &&
                vm.ShowDamageIn
                    ? 1
                    : 0;

            debuffWindow.Opacity =
                vm.OverlayEnabled &&
                vm.ShowDebuffs
                    ? 1
                    : 0;


            //var overlay =
            //    new CombatOverlayWindow
            //    {
            //        DataContext = vm
            //    };

            //overlay.Show();
        }
        base.OnFrameworkInitializationCompleted();
    }
}