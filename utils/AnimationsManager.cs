using System.Collections.Generic;
using Godot;
using Newtonsoft.Json.Linq;
using System.IO;

public class AnimationsManager{

	private class SpriteSheetData{
		public string Name;
		public Texture2D Texture;
		public int FrameSizeX;
		public int FrameSizeY;
		public int MaxFramesX;
		public int MaxFramesY;

		public SpriteSheetData(JObject jsonData){
			this.Name = (string)jsonData["name"];
			this.Texture = (Texture2D)GD.Load((string)jsonData["filepath"]);
			GD.Print(this.Name + ", " + Texture.GetSize());
			this.FrameSizeX = (int)jsonData["frame_size_x"];
			this.FrameSizeY = (int)jsonData["frame_size_y"];
			if (this.Texture.GetSize().X % this.FrameSizeX != 0){
				GD.Print($"WARNING!!!!! sprite sheet {this.Name}, size {this.Texture.GetSize()} is not divisible by frame size x ({this.FrameSizeX})");
			}

			if (this.Texture.GetSize().Y % this.FrameSizeY != 0){
				GD.Print($"WARNING!!!!! sprite sheet {this.Name}, size {this.Texture.GetSize()} is not divisible by frame size y ({this.FrameSizeY})");
			}

			this.MaxFramesX = (int)(this.Texture.GetSize().X / this.FrameSizeX);
			this.MaxFramesY = (int)(this.Texture.GetSize().Y / this.FrameSizeY);
		}
	}

	public static Dictionary<string, SpriteFrames> LoadAllSpriteFrames(string directoryPath){
		Dictionary<string, SpriteFrames> allSpriteFrames = new Dictionary<string, SpriteFrames>();
		string[] files = Directory.GetFiles(Godot.ProjectSettings.GlobalizePath(directoryPath));
		foreach (string file in files){
			Dictionary<string, SpriteFrames> frames = LoadSpriteFrames(file);
			foreach (var f in frames){
				allSpriteFrames.Add(f.Key, f.Value);
			}
		}

		return allSpriteFrames;
	}

	public static Dictionary<string, SpriteFrames> LoadSpriteFrames(string configPath){
		/*
			Assumptions / Current Limitations:
				- All frames in a given sprite sheet are the same size.
				- A single animation does not span multiple rows in a given sheet.
				- Logical sprites (group of animations) do not span multiple sheets.

			Format:
				"sheets": [
					{
						"name": "<unique ID>",
						"filepath": "res://assets/ ... ",
						"frame_size_x": <x pixels per frame>,
						"frame_size_y": <y pixels per frame>
					},
					...
				]
				"sprites":[
					{
						"name": "<unique ID>",
						"sheet": "<name of a sheet from 'sheets'>",
						"animations":[
							{
								"name": "<unique to this sprite, name used when calling AnimatedSprite2D.Play(<name>)>",
								"frame_start_x": <frame index, NOT pixel>,
								"frame_start_y": <frame index, NOT pixel>,
								"num_frames": <OPTIONAL (default 1) - number of frames in x direction>,
								"looped": <OPTIONAL (default false) - if the animation is looping>
							},
							...
					},
					...
				]
		*/
		Dictionary<string, SpriteSheetData> spriteSheetData = new Dictionary<string, SpriteSheetData>();
		Dictionary<string, SpriteFrames> spriteFrames = new Dictionary<string, SpriteFrames>();

		JObject animationData = JObject.Parse(File.ReadAllText(Godot.ProjectSettings.GlobalizePath(configPath)));
		JArray sheets = (JArray)animationData["sheets"];
		foreach (JObject spriteSheet in sheets){
			string name = (string)spriteSheet["name"];
			spriteSheetData.Add(name, new SpriteSheetData(spriteSheet));
		}

		JArray sprites = (JArray)animationData["sprites"];
		foreach (JObject sprite in sprites){
			string spriteName = (string)sprite["name"];
			SpriteFrames frames = new SpriteFrames();
			SpriteSheetData data = spriteSheetData[(string)sprite["sheet"]];
			Texture2D texture = data.Texture;
			JArray animations = (JArray)sprite["animations"];
			foreach (JObject animation in animations){
				string animationName = (string)animation["name"];
				frames.AddAnimation(animationName);

				bool looped = false;

				if (animation.ContainsKey("looped")){
					looped = (bool)animation["looped"];
				}

				frames.SetAnimationLoop(animationName, looped);
				int frameStartX = (int)animation["frame_start_x"];
				int frameStartY = (int)animation["frame_start_y"];

				int numFrames = 1;
				if (animation.ContainsKey("num_frames")){
					numFrames = (int)animation["num_frames"];
				}

				if (numFrames + frameStartX > data.MaxFramesX){
					throw new System.Exception($"Animation frames in {spriteName} sprite are out of bounds, frame_start_x + num_frames={frameStartX + numFrames}, max is {data.MaxFramesX}");
				}

				if (frameStartY >= data.MaxFramesY){
					throw new System.Exception($"Animation frames in {spriteName} sprite are out of bounds, frame_start_y={frameStartY}, max is {data.MaxFramesY - 1}");
				}

				int coordinateStartX = frameStartX * data.FrameSizeX;
				int coordinateStartY = frameStartY * data.FrameSizeY;

				for (int i = 0; i < numFrames; i++){
					AtlasTexture atlas = new AtlasTexture();
					atlas.Atlas = texture;
					atlas.Region = new Rect2(coordinateStartX + (i * data.FrameSizeX),
											 coordinateStartY,
											 data.FrameSizeX,
											 data.FrameSizeY);
					frames.AddFrame(animationName, atlas);
				}

				spriteFrames[spriteName] = frames;
			}
		}

		return spriteFrames;
	}
}
