using Godot;
using Steamworks.Data;
using System;
using System.Data;
using System.Diagnostics.Tracing;

public partial class MainMenu : Control
{
	private enum SubMenu{
		NONE,
		OPTIONS
	}

	private SubMenu CurrentSubMenu {get; set;}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		CurrentSubMenu = SubMenu.NONE;
		if (SteamManager.Manager.SteamRunningInOnlineMode){
			GD.Print("MM: Connected to steam! " + SteamManager.Manager.PlayerName);
		} else {
			GD.Print("MM: Not connected to steam! " + SteamManager.Manager.PlayerName);
		}
	}

	private async void _on_SingleplayerButton_pressed()
	{
		//GetTree().ChangeSceneToFile("res://scenes/main_menu/LobbySceneManager.tscn");
		GD.Print("MM: single player ");
	}

	private void _on_options_button_button_down()
	{
		// TODO this is just for testing achievements
		SteamManager.Manager.UnlockSteamAchievement(SteamAchievementsApi.AchievementId.TEST_ACHIEVEMENT_1);
		SteamManager.Manager.IncrementSteamAchievementStat(SteamAchievementsApi.StatId.OPTIONS_BUTTON_PRESSED);

		GetTree().ChangeSceneToFile("res://scenes/main_menu/Options.tscn");
	}
	
	private void _on_QuitButton_pressed()
	{
		SteamManager.Manager.__DebugClearAllStats();
		GetTree().Quit();
	}
}
