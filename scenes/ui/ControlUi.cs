using Godot;
using System;

public partial class ControlUi : Control
{
	private float cash;
	public ControlUi()
	{
		this.cash = 0f;
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		SetTimeRemaining(0);
		AddCash(0);

	}

	public void SetTimeRemaining(double secondsRemaining)
	{
		string timeString = TimeSpan.FromSeconds(secondsRemaining).ToString(@"mm\:ss\.ff");
		GetNode<RichTextLabel>("TimeUntilNextChunkSpawnLabel").Text = "T-" + timeString;
	}

	public void AddCash(float cash)
	{
		this.cash += cash;
		GetNode<RichTextLabel>("MoneyLabel").Text = this.cash.ToString("C");
	}
}
