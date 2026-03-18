using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

public partial class Options : Node2D
{

	readonly Dictionary<string, Vector2I> SUPPORTED_RESOLUTIONS = new Dictionary<string, Vector2I>{
		{"1920x1080", new Vector2I(1920, 1080)},
		{"1600x900", new Vector2I(1600, 900)},
		{"1440x900", new Vector2I(1440, 900)},
		{"1366x768", new Vector2I(1366, 768)},
		{"1280x800", new Vector2I(1280, 800)},
		{"1024x800", new Vector2I(1024, 800)},
		{"800x600", new Vector2I(800, 600)}
	};

	readonly Dictionary<string, DisplayServer.WindowMode> WINDOW_MODES = new Dictionary<string, DisplayServer.WindowMode>(){
		{"Fullscreen", DisplayServer.WindowMode.Fullscreen},
		{"Windowed", DisplayServer.WindowMode.Windowed}
	};

	private long resolutionSelected;
	private long windowModeSelected;

	public Options(){
		this.resolutionSelected = 0;
		this.windowModeSelected = 0;
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		OptionButton resolutions = GetNode<OptionButton>("OptionSections/ResolutionContainer/Resolution") as OptionButton;
		foreach (var i in SUPPORTED_RESOLUTIONS){
			resolutions.AddItem(i.Key);
		}

		resolutions.Select(0);

		OptionButton windowModes = GetNode<OptionButton>("OptionSections/WindowModeContainer/WindowMode") as OptionButton;
		foreach (var i in WINDOW_MODES){
			windowModes.AddItem(i.Key);
		}

		windowModes.Select(0);
	}

	private void _on_resolution_item_selected(long index)
	{
		this.resolutionSelected = index;
		Globals.Settings.SetGraphicsSettings(SUPPORTED_RESOLUTIONS.Values.ToList()[(int)this.resolutionSelected],
											 WINDOW_MODES.Values.ToList()[(int)this.windowModeSelected]);
	}

	private void _on_window_mode_item_selected(long index)
	{
		this.windowModeSelected = index;
		Globals.Settings.SetGraphicsSettings(SUPPORTED_RESOLUTIONS.Values.ToList()[(int)this.resolutionSelected],
											 WINDOW_MODES.Values.ToList()[(int)this.windowModeSelected]);
	}

	private void _on_apply_button_button_down()
	{
		Globals.Settings.SaveAndSetGraphicsSettings(SUPPORTED_RESOLUTIONS.Values.ToList()[(int)this.resolutionSelected],
													WINDOW_MODES.Values.ToList()[(int)this.windowModeSelected]);
	}

	private void _on_back_button_button_down()
	{
		GetTree().ChangeSceneToFile("res://scenes/main_menu/MainMenu.tscn");
	}
}
