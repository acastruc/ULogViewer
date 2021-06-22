﻿using System;
namespace CarinaStudio.ULogViewer.Logs.DataSources
{
	/// <summary>
	/// Implementation of <see cref="StandardOutputLogDataSourceProvider"/>.
	/// </summary>
	class StandardOutputLogDataSourceProvider : BaseLogDataSourceProvider
	{
		/// <summary>
		/// Initialize new <see cref="StandardOutputLogDataSourceProvider"/> instance.
		/// </summary>
		/// <param name="app"><see cref="App"/>.</param>
		public StandardOutputLogDataSourceProvider(App app) : base(app)
		{
		}


		// Interface implementations.
		protected override ILogDataSource CreateSourceCore(LogDataSourceOptions options) => new StandardOutputLogDataSource(this, options);
		public override string Name => "StandardOutput";
		public override UnderlyingLogDataSource UnderlyingSource => UnderlyingLogDataSource.StandardOutput;
	}
}
