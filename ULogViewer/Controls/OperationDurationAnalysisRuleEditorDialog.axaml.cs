using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using CarinaStudio.AppSuite.Controls;
using CarinaStudio.Collections;
using CarinaStudio.Threading;
using CarinaStudio.ULogViewer.ViewModels.Analysis;
using CarinaStudio.ULogViewer.ViewModels.Analysis.ContextualBased;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;

namespace CarinaStudio.ULogViewer.Controls
{
	/// <summary>
	/// Dialog to edit <see cref="OperationDurationAnalysisRuleSet.Rule"/>.
	/// </summary>
	partial class OperationDurationAnalysisRuleEditorDialog : AppSuite.Controls.InputDialog<IULogViewerApplication>
	{
		// Fields.
		readonly ObservableList<ContextualBasedAnalysisCondition> beginningConditions = new();
		readonly PatternEditor beginningPatternEditor;
		readonly ObservableList<ContextualBasedAnalysisAction> beginningPostActions = new();
		readonly ObservableList<ContextualBasedAnalysisAction> beginningPreActions = new();
		readonly TextBox customMessageTextBox;
		readonly ObservableList<ContextualBasedAnalysisCondition> endingConditions = new();
		readonly ComboBox endingModeComboBox;
		readonly PatternEditor endingPatternEditor;
		readonly ObservableList<ContextualBasedAnalysisAction> endingPostActions = new();
		readonly ObservableList<ContextualBasedAnalysisAction> endingPreActions = new();
		readonly Avalonia.Controls.ListBox endingVariableListBox;
		readonly ObservableList<string> endingVariables = new();
		readonly CarinaStudio.Controls.TimeSpanTextBox maxDurationTextBox;
		readonly CarinaStudio.Controls.TimeSpanTextBox minDurationTextBox;
		readonly TextBox operationNameTextBox;
		readonly ComboBox resultTypeComboBox;


		/// <summary>
		/// Initialize new <see cref="OperationDurationAnalysisRuleEditorDialog"/> instance.
		/// </summary>
		public OperationDurationAnalysisRuleEditorDialog()
		{
			AvaloniaXamlLoader.Load(this);
			this.beginningPatternEditor = this.Get<PatternEditor>(nameof(beginningPatternEditor)).Also(it =>
			{
				it.GetObservable(PatternEditor.PatternProperty).Subscribe(_ => this.InvalidateInput());
			});
			this.customMessageTextBox = this.Get<TextBox>(nameof(customMessageTextBox));
			this.endingModeComboBox = this.Get<ComboBox>(nameof(endingModeComboBox));
			this.endingPatternEditor = this.Get<PatternEditor>(nameof(endingPatternEditor)).Also(it =>
			{
				it.GetObservable(PatternEditor.PatternProperty).Subscribe(_ => this.InvalidateInput());
			});
			this.endingVariableListBox = this.Get<AppSuite.Controls.ListBox>(nameof(endingVariableListBox)).Also(it =>
			{
				it.DoubleClickOnItem += (_, e) => this.EditEndingVariable((string)e.Item);
			});
			this.maxDurationTextBox = this.Get<CarinaStudio.Controls.TimeSpanTextBox>(nameof(maxDurationTextBox)).Also(it =>
			{
				it.GetObservable(CarinaStudio.Controls.TimeSpanTextBox.IsTextValidProperty).Subscribe(_ => this.InvalidateInput());
				it.GetObservable(CarinaStudio.Controls.TimeSpanTextBox.ValueProperty).Subscribe(_ => this.InvalidateInput());
			});
			this.minDurationTextBox = this.Get<CarinaStudio.Controls.TimeSpanTextBox>(nameof(minDurationTextBox)).Also(it =>
			{
				it.GetObservable(CarinaStudio.Controls.TimeSpanTextBox.IsTextValidProperty).Subscribe(_ => this.InvalidateInput());
				it.GetObservable(CarinaStudio.Controls.TimeSpanTextBox.ValueProperty).Subscribe(_ => this.InvalidateInput());
			});
			this.operationNameTextBox = this.Get<TextBox>(nameof(operationNameTextBox)).Also(it =>
			{
				it.GetObservable(TextBox.TextProperty).Subscribe(_ => this.InvalidateInput());
			});
			this.resultTypeComboBox = this.Get<ComboBox>(nameof(resultTypeComboBox));
		}


		// Add ending variable.
		async void AddEndingVariable()
		{
			var variable = await new TextInputDialog()
			{
				MaxTextLength = 256,
				Message = this.Application.GetString("OperationDurationAnalysisRuleEditorDialog.EndingVariable"),
			}.ShowDialog(this);
			if (string.IsNullOrWhiteSpace(variable))
				return;
			variable = variable.Trim();
			var index = this.endingVariables.IndexOf(variable);
			if (index < 0)
			{
				this.endingVariables.Add(variable);
				this.endingVariableListBox.SelectedIndex = this.endingVariables.Count - 1;
			}
			else
				this.endingVariableListBox.SelectedIndex = index;
			this.endingVariableListBox.Focus();
		}


		// Beginning conditions.
		IList<ContextualBasedAnalysisCondition> BeginningConditions { get => this.beginningConditions; }


		// Post-actions for beginning of operation.
		IList<ContextualBasedAnalysisAction> BeginningPostActions { get => this.beginningPostActions; }


		// Pre-actions for beginning of operation.
		IList<ContextualBasedAnalysisAction> BeginningPreActions { get => this.beginningPreActions; }


		// Edit ending variable.
		void EditEndingVariable(ListBoxItem item) =>
			this.EditEndingVariable((string)item.DataContext.AsNonNull());
		async void EditEndingVariable(string variable)
		{
			var index = this.endingVariables.IndexOf(variable);
			if (index < 0)
				return;
			var newVariable = await new TextInputDialog()
			{
				InitialText = variable,
				MaxTextLength = 256,
				Message = this.Application.GetString("OperationDurationAnalysisRuleEditorDialog.EndingVariable"),
			}.ShowDialog(this);
			if (string.IsNullOrWhiteSpace(newVariable))
				return;
			newVariable = newVariable.Trim();
			if (newVariable == variable)
				return;
			this.endingVariables[index] = newVariable;
			this.endingVariableListBox.SelectedIndex = index;
			this.endingVariableListBox.Focus();
		}


		// Ending conditions.
		IList<ContextualBasedAnalysisCondition> EndingConditions { get => this.endingConditions; }


		// Post-actions for ending of operation.
		IList<ContextualBasedAnalysisAction> EndingPostActions { get => this.endingPostActions; }


		// Pre-actions for ending of operation.
		IList<ContextualBasedAnalysisAction> EndingPreActions { get => this.endingPreActions; }


		// Ending variables.
		IList<string> EndingVariables { get => this.endingVariables; }


		// Generate result.
		protected override Task<object?> GenerateResultAsync(CancellationToken cancellationToken)
		{
			var rule = this.Rule;
			var newRule = new OperationDurationAnalysisRuleSet.Rule(this.operationNameTextBox.Text.AsNonNull(),
				(DisplayableLogAnalysisResultType)this.resultTypeComboBox.SelectedItem.AsNonNull(),
				this.beginningPatternEditor.Pattern.AsNonNull(),
				this.beginningPreActions,
				this.beginningConditions,
				this.beginningPostActions,
				this.endingPatternEditor.Pattern.AsNonNull(),
				this.endingPreActions,
				this.endingConditions,
				this.endingPostActions,
				(OperationEndingMode)this.endingModeComboBox.SelectedItem.AsNonNull(),
				this.endingVariables,
				this.minDurationTextBox.Value,
				this.maxDurationTextBox.Value,
				this.customMessageTextBox.Text);
			if (rule == newRule)
				return Task.FromResult<object?>(rule);
			return Task.FromResult<object?>(newRule);
		}


		// Dialog opened.
		protected override void OnOpened(EventArgs e)
		{
			base.OnOpened(e);
			var rule = this.Rule;
			if (rule != null)
			{
				this.operationNameTextBox.Text = rule.OperationName;
				this.resultTypeComboBox.SelectedItem = rule.ResultType;
				this.beginningConditions.AddAll(rule.BeginningConditions);
				this.beginningPatternEditor.Pattern = rule.BeginningPattern;
				this.beginningPreActions.AddAll(rule.BeginningPreActions);
				this.beginningPostActions.AddAll(rule.BeginningPostActions);
				this.customMessageTextBox.Text = rule.CustomMessage;
				this.endingConditions.AddAll(rule.EndingConditions);
				this.endingModeComboBox.SelectedItem = rule.EndingMode;
				this.endingPatternEditor.Pattern = rule.EndingPattern;
				this.endingPreActions.AddAll(rule.EndingPreActions);
				this.endingPostActions.AddAll(rule.EndingPostActions);
				this.endingVariables.AddAll(rule.EndingVariables);
				this.minDurationTextBox.Value = rule.MinDuration;
				this.maxDurationTextBox.Value = rule.MaxDuration;
			}
			else
			{
				this.resultTypeComboBox.SelectedItem = DisplayableLogAnalysisResultType.TimeSpan;
				this.endingModeComboBox.SelectedItem = OperationEndingMode.FirstInFirstOut;
			}
			this.SynchronizationContext.Post(() =>
			{
				if (!this.beginningPatternEditor.ShowTutorialIfNeeded(this.Get<TutorialPresenter>("tutorialPresenter"), this.operationNameTextBox))
					this.operationNameTextBox.Focus();
			});
		}


		// Validate input.
		protected override bool OnValidateInput() =>
			base.OnValidateInput() 
			&& !string.IsNullOrWhiteSpace(this.operationNameTextBox.Text) 
			&& this.beginningPatternEditor.Pattern != null
			&& this.endingPatternEditor.Pattern != null
			&& this.minDurationTextBox.IsTextValid
			&& this.maxDurationTextBox.IsTextValid
			&& this.minDurationTextBox.Value.Let(minDuration =>
			{
				return this.maxDurationTextBox.Value.Let(maxDuration =>
				{
					if (!minDuration.HasValue || !maxDuration.HasValue)
						return true;
					return minDuration <= maxDuration;
				});
			});
		

		// Remove ending variable.
		void RemoveEndingVariable(ListBoxItem item)
		{
			var variable = (string)item.DataContext.AsNonNull();
			this.endingVariables.Remove(variable);
			this.endingVariableListBox.SelectedItem = null;
			this.endingVariableListBox.Focus();
		}


		// Available result types.
		DisplayableLogAnalysisResultType[] ResultTypes { get; } = Enum.GetValues<DisplayableLogAnalysisResultType>();
		

		/// <summary>
		/// Get or set rule to be edited.
		/// </summary>
		public OperationDurationAnalysisRuleSet.Rule? Rule { get; set; }
	}
}
