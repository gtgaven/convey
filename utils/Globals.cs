using Godot;
using System;
using System.Collections.Generic;

public interface IMetadatable {
	public string GetIdentifier();
	public string GetName();
	public string GetDescription();
}

public interface IDisplayable {
	public string GetSpriteFramesName();
	public string GetDefaultAnimationName();
}

public interface IPurchasable : IMetadatable, IDisplayable {
	public float GetCostToBuy();
	public int GetBuyStackSize();
}

public class DifficultySettings{
	public float Setting1 {get; set;}
	public float Setting2 {get; set;}
	private string name;

	public DifficultySettings(string section, ConfigFile config){
		this.name = section;
		this.Setting1 = (float)config.GetValue(section, "setting1");
		this.Setting2 = (float)config.GetValue(section, "setting2");
	}

	public override string ToString()
	{
		return $"{name}:\n- setting1: {Setting1} setting2: {Setting2}";
	}
}

public partial class Globals : Node
{
	private readonly string ANIMATIONS_FOLDER = "res://config/animations";
	public readonly string DEFAULT_DIFFICULTY_INI_PATH = "res://config/Difficulty.ini";
	private readonly string SAVED_OPTIONS_INI_PATH = "res://config/Options.ini";
	public static Globals Settings {get; set;}

	public RandomNumberGenerator rng;

	public Dictionary<string, DifficultySettings> Difficulties;

	private Dictionary<string, SpriteFrames> animatedSpriteFrames;

	//public Inventory PersistentInventory;

	public Globals(){
		if (Settings != null){
			return;
		}


		Settings = this;
		rng = new RandomNumberGenerator();
		Difficulties = new Dictionary<string, DifficultySettings>();

		OS.GetCmdlineArgs();
		Dictionary<string, string> args = new Dictionary<string, string>();

		foreach (var argument in OS.GetCmdlineArgs()) {
			if (argument.Find("=") > -1) { 
				string[] keyValue = argument.Split("="); 
				args[keyValue[0]] = keyValue[1]; 
			} else {
				// Options without an argument will be present in the dictionary, 
				// with the value set to an empty string. 
				args[argument] = "";
			}
		}

		foreach (var i in args){
			GD.Print(i.Key + " was set to " + i.Value);
		}

		string difficultyIniFilePath = DEFAULT_DIFFICULTY_INI_PATH;

		if (args.ContainsKey("--difficulty_ini")){
			difficultyIniFilePath = args["--difficulty_ini"];
		}

		InitializeDifficultySettings(difficultyIniFilePath);
		
		this.animatedSpriteFrames = AnimationsManager.LoadAllSpriteFrames(ANIMATIONS_FOLDER);
		//this.PersistentInventory = InventoryManager.LoadInventoryFromFile("res://config/inventory.json");
	}

	public override void _Ready()
	{
		InitializeOptions(SAVED_OPTIONS_INI_PATH);
	}

	public SpriteFrames GetSpriteFrames(string name){
		if (this.animatedSpriteFrames.ContainsKey(name)){
			return this.animatedSpriteFrames[name];
		}

		return this.animatedSpriteFrames["placeholder"];
	}

	private void InitializeOptions(string optionsIniPath){
		ConfigFile optionsConfigFile = new ConfigFile();
		Error error = optionsConfigFile.Load(optionsIniPath);
		if (error != Error.Ok){
			SetGraphicsSettings(new Vector2I(1920, 1080), DisplayServer.WindowMode.Fullscreen);
			return;
		}
		Vector2I resolution = (Vector2I)optionsConfigFile.GetValue("graphics", "resolution");
		bool fullscreen = (bool)optionsConfigFile.GetValue("graphics", "fullscreen");

		if (fullscreen){
			this.SetGraphicsSettings(resolution, DisplayServer.WindowMode.Fullscreen);
		} else {
			this.SetGraphicsSettings(resolution, DisplayServer.WindowMode.Windowed);
		}
	}

	private void InitializeDifficultySettings(string difficultyIniPath){
		ConfigFile DifficultyConfigFile = new ConfigFile();
		Error error = DifficultyConfigFile.Load(difficultyIniPath);
		if (error != Error.Ok){
			GD.Print($"failed to load difficulty settings from {difficultyIniPath}");
			throw new Exception("Invalid difficulty .ini file: " + difficultyIniPath);
		}

		foreach (string difficulty in DifficultyConfigFile.GetSections()){
			this.Difficulties.Add(difficulty, new DifficultySettings(difficulty, DifficultyConfigFile));
		}
	}

	public void SetGraphicsSettings(Vector2I resolution, DisplayServer.WindowMode windowMode){
		DisplayServer.WindowSetMode(windowMode);
		DisplayServer.WindowSetSize(resolution);
		GetViewport().GetWindow().ContentScaleSize = resolution;
	}

	public void SaveAndSetGraphicsSettings(Vector2I resolution, DisplayServer.WindowMode windowMode){
		this.SetGraphicsSettings(resolution, windowMode);
		ConfigFile optionsConfigFile = new ConfigFile();
		Error error = optionsConfigFile.Load(SAVED_OPTIONS_INI_PATH);
		optionsConfigFile.SetValue("graphics", "resolution", resolution);
		if (windowMode == DisplayServer.WindowMode.Fullscreen){
			optionsConfigFile.SetValue("graphics", "fullscreen", true);
		} else {
			optionsConfigFile.SetValue("graphics", "fullscreen", false);
		}
		
		optionsConfigFile.Save(SAVED_OPTIONS_INI_PATH);
	}
}
