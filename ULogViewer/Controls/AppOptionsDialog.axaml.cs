using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;
using CarinaStudio.Threading;
using CarinaStudio.ULogViewer.ViewModels;
using System;
using System.ComponentModel;

namespace CarinaStudio.ULogViewer.Controls
{
	/// <summary>
	/// Dialog for application options.
	/// </summary>
	partial class AppOptionsDialog : AppSuite.Controls.Dialog<IULogViewerApplication>
	{
		// Static fields.
		public static readonly IValueConverter ThemeModeConverter = new AppSuite.Converters.EnumConverter(App.Current, typeof(AppSuite.ThemeMode));


		// Fields.
		readonly ScheduledAction refreshDataContextAction;


		/// <summary>
		/// Initialize new <see cref="AppOptionsDialog"/> instance.
		/// </summary>
		public AppOptionsDialog()
		{
			this.DataContext = new AppOptions();
			InitializeComponent();
			this.Application.PropertyChanged += this.OnApplicationPropertyChanged;
			this.Application.StringsUpdated += this.OnApplicationStringsUpdated;
			this.refreshDataContextAction = new ScheduledAction(() =>
			{
				var dataContext = this.DataContext;
				this.DataContext = null;
				this.DataContext = dataContext;
			});
		}

		
		// Initialize.
		private void InitializeComponent() => AvaloniaXamlLoader.Load(this);


		// Called when application property changed.
		void OnApplicationPropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(IULogViewerApplication.EffectiveThemeMode))
				this.refreshDataContextAction.Reschedule(500);
		}


		// Called when strings updated.
		void OnApplicationStringsUpdated(object? sender, EventArgs e)
		{
			this.refreshDataContextAction.Schedule();
		}


		// Called when closed.
		protected override void OnClosed(EventArgs e)
		{
			this.Application.PropertyChanged -= this.OnApplicationPropertyChanged;
			this.Application.StringsUpdated -= this.OnApplicationStringsUpdated;
			this.refreshDataContextAction.Cancel();
			(this.DataContext as AppOptions)?.Dispose();
			this.DataContext = null;
			base.OnClosed(e);
		}
	}
}
