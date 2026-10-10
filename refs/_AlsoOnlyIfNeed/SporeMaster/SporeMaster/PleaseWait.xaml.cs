using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Markup;

namespace SporeMaster;

public partial class PleaseWait : Window, IComponentConnector
{
	public delegate void Operation(PleaseWait progress);

	public delegate void SimpleDelegate();

	private bool cancelled;

	private List<double> subTaskScale = new List<double> { 1.0 };

	private List<double> subTaskEnd = new List<double> { 1.0 };

	public PleaseWait(Window parent, string operationDetails, Operation operation)
	{
		PleaseWait pleaseWait = this;
		InitializeComponent();
		OperationDetails.Text = operationDetails;
		IsVisibleChanged += PleaseWait_IsVisibleChanged;
		Owner = parent;
		ProgressBar.Maximum = 1.0;
		ThreadStart start = () =>
		{
			try
			{
				operation(pleaseWait);
			}
			catch (OperationCanceledException)
			{
			}
			catch (Exception ex2)
			{
				MessageBox.Show(ex2.ToString(), "Error");
			}
			pleaseWait.complete();
		};
		new Thread(start).Start();
	}

	private void PleaseWait_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
	{
		if (!IsVisible)
		{
			cancelled = true;
		}
	}

	public void beginTask(double progressInCurrentTask, double progressMaximum)
	{
		Dispatcher.BeginInvoke((SimpleDelegate)(() =>
		{
			double num = subTaskScale[subTaskScale.Count - 1];
			subTaskScale.Add(num * progressInCurrentTask / progressMaximum);
			subTaskEnd.Add(ProgressBar.Value + num * progressInCurrentTask);
		}));
	}

	public void endTask()
	{
		Dispatcher.BeginInvoke((SimpleDelegate)(() =>
		{
			int index = subTaskScale.Count - 1;
			ProgressBar.Value = subTaskEnd[index];
			subTaskEnd.RemoveAt(index);
			subTaskScale.RemoveAt(index);
		}));
	}

	public void addProgress(double amount)
	{
		Dispatcher.BeginInvoke((SimpleDelegate)(() =>
		{
			ProgressBar.Value += amount * subTaskScale[subTaskScale.Count - 1];
		}));
		if (cancelled)
		{
			throw new OperationCanceledException("Cancelled by user.");
		}
	}

	public void complete()
	{
		Dispatcher.BeginInvoke((SimpleDelegate)(() =>
		{
			Close();
		}));
	}

	private void Cancel_Click(object sender, RoutedEventArgs e)
	{
		cancelled = true;
	}

	[DebuggerNonUserCode]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocater = new Uri("/SporeMaster;component/pleasewait.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocater);
		}
	}
}
