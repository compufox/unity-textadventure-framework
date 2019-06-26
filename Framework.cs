//-----------------------------------------------------------------------------
//    A simple framework for easing the creation of text based
//      games in the Unity Game Engine
//    Copyright (C) 2019  theZacAttacks
//
//    This program is free software: you can redistribute it and/or modify
//    it under the terms of the GNU General Public License as published by
//    the Free Software Foundation, either version 3 of the License, or
//    (at your option) any later version.
//
//    This program is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//    GNU General Public License for more details.
//
//    You should have received a copy of the GNU General Public License
//    along with this program.  If not, see <https://www.gnu.org/licenses/>.
//-----------------------------------------------------------------------------

using System.Linq;
using System;
using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#pragma warning disable CS0414

namespace AdventureFramework {
    public class Framework : MonoBehaviour {
	
	private const string UnlockError = "I can't unlock a door that isn't locked!\nCheck and make sure that the direction you want unlocked ({0}) is locked in room [{1}][{2}]";
	private const string LockError = "I can't lock a door that isn't unlocked!\nCheck and make sure that the direction you want locked ({0}) is unlocked in room [{1}][{2}]";
	private const string ActionHeader1 = "The framework supports the following built-in commands:";
	private const string ActionHeader2 = "   go, look, examine, use, quit, help, clear, play, take, and inventory";
	
	private static bool GAME_OVER = false;
	private static Text _output;
	
	
	public InputField inputField;
	public Text outputArea;

	[TextArea] [SerializeField] private string start;
	
	public int startingColumn = 0;
	public int startingRow = 0;

	[Header(ActionHeader1)]
	[Space(-10)]
	[Header(ActionHeader2)]
	[Space(6)]
	public string actionNotFoundMessage;
	private string[] actions =
	    { "go", "look", "examine", "use", "quit",
	      "help", "clear", "play", "take", "inventory" };
	
	[SerializeField]
	private RoomRow[] columns;
	
	private int x = 0, y = 0;
	private static List<Item> backpack = new List<Item>();
	
	private string[] inputActionData;
	
	private bool started = false;
	private Dictionary<string,FrameworkCommand> userActions;
	
	void Awake() {
	    Framework._output = outputArea;
	    
	    if (startingColumn < 0 || startingRow < 0) {
		throw new FrameworkError("Negative Rooms don't exist!");
	    } else if (columns.Length <= startingColumn || 
		       columns[startingColumn].row.Length <= startingRow) {
		throw new FrameworkError("That room doesn't exist!");
	    } else {
		y = startingColumn;
		x = startingRow;
	    }
	    
	    FrameworkCommand[] commands = GetComponents<FrameworkCommand>();
	    
	    if (commands.Length > 0) {
		userActions = new Dictionary<string, FrameworkCommand>();
		
		foreach(FrameworkCommand cmd in commands) {
		    userActions.Add(cmd.GetType().Name, cmd);
		}
	    }
	    
	    inputField.ActivateInputField();
	    
	    LogString(start);
	}
	
	public static void LogString(string str) {
	    _output.text += str + "\n";
	}
	
	public void ProcessInput(string input) {
	    input = input.ToLower().Trim();
	    
	    LogString(input);
	    
	    if (!GAME_OVER) {
		
		var splitInput = input.ToLower().Split(' ');
		input = splitInput[0];
		inputActionData = splitInput.Where((source, index) => index != 0).ToArray();
		
		
		if (actions.Contains(input) && started) {
		    Invoke(input, 0f);
		    
		} else if (!started) {
		    if (input == "play") {
			Invoke(input, 0f);
			
		    } else {
			LogString("\nYou need to start the game...");
		    }
		    
		} else if (userActions != null && userActions.ContainsKey(input)) {
		    userActions[input].run(inputActionData, backpack);
			
		} else {
		    LogString("\n" + actionNotFoundMessage);
		}
		
	    } else if (input == "quit") {
		quit();
	    }
	    
	    InputComplete();
	}
	
	void InputComplete() {
	    inputField.ActivateInputField();
	    inputField.text = null;
	}
	
	private void PrintRoom(bool printLong = false) {
	    Room cur = columns[y].row[x];
	    string output;
	    
	    if (printLong) {
		output = cur.longDescription;
	    } else {
		output = cur.shortDescription;
	    }
	    
	    output += "\nThere are exits to the ";
	    
	    for (int i = 0; i < cur.connections.Length; i++)
		if (i == cur.connections.Length - 1) {
		    //		output += "and " + cur.connections[i].ToString().ToLower() + " ";
		    //	    else
		    output += cur.connections[i].ToString().ToLower() + " ";
		} else {
		    output += cur.connections[i].ToString().ToLower() + ", ";
		}
	    
	    if (cur.lockedConnections.Length > 0) {
		output += "\nThere are locked exits to the ";
		for (int i = 0; i < cur.lockedConnections.Length; i++) {
		    output += cur.lockedConnections[i].ToString().ToLower() + " ";
		}
	    }
	    
	    if (cur.items.Length > 0) {
		output += "\nYou see ";
		if (cur.items.Length > 1) {
		    for (int i = 0; i < cur.items.Length; i++) {
			if (i == cur.items.Length - 1) {
			    output += "and a " + cur.items[i].name + " ";
			} else {
			    output += "a " + cur.items[i].name + ", ";
			}
		    }
		} else {
		    output += "a " + cur.items[0].name + " ";
		}
		
		output += "on the ground";
	    }
	    
	    LogString("\n" + output);
	    
	}
	
	public static bool InInventory(string item) {
	    for (int i = 0; i < backpack.Count; i++) {
		if (backpack[i].name == item) {
		    return true;
		}
	    }
	    
	    return false;
	}
	
	private void UseHelper(TextEvent r) {
	    if (r.effectOtherRoom) {
		columns[r.roomCoords[0]]
		    .row[r.roomCoords[1]]
		    .processEvent(r);
	    }
	    
	    if (!string.IsNullOrEmpty(r.getReaction())) {
		LogString("\n" + r.getReaction());
	    }
	}
	
	private void quit() {
	    Application.Quit();
	}
	
	private void play() {
	    actions = actions.Where((item, index) => item != "play").ToArray();
	    started = true;
	    PrintRoom();
	}
	
	private void go() {
	    if (inputActionData.Length > 0) {
		try {
		    Room.Direction dir = (Room.Direction) Enum.Parse(typeof(Room.Direction),
								     inputActionData[0],
								     true);
		    
		    if (columns[y].row[x].validDirection(dir)) {
			switch (dir) {
			    case Room.Direction.NORTH:
				y += 1;
				break;
			    case Room.Direction.EAST:
				x += 1;
				break;
			    case Room.Direction.WEST:
				x -= 1;
				break;
			    case Room.Direction.SOUTH:
				y -= 1;
				break;
			}
			
			PrintRoom();
		    } else if (columns[y].row[x].isLockedDirection(dir)) {
			LogString("\nThat way is locked");
		    } else {
			LogString("\nYou can't walk through walls");
		    }
		    
		} catch (System.ArgumentException e) {
		    LogString("\nI'm sorry, but that isn't a direction");
		}
	    } else {
		LogString("\nGo where?");
	    }
	    
	}
	
	private void take() {
	    if (inputActionData.Length > 0) {
		Room cur = columns[y].row[x];
		string item = inputActionData[0];
		int itemIndex = cur.itemInRoom(item);
		
		
		if (itemIndex > -1) {
		    backpack.Add(cur.items[itemIndex]);
		    cur.items = cur.items.Where((source, index) => index != itemIndex).ToArray();
		    LogString("\nPicked up " + item);
		    columns[y].row[x] = cur;
		} else
		    LogString("\nthere is no " + item + " in here");
	    } else
		LogString("\ntake what?");
	}
	
	private void inventory() {
	    LogString("\nIn your bag you see:");
	    
	    if (backpack.Count > 0) {
		foreach (Item i in backpack) {
		    LogString("\ta " + i.name);
		}
	    } else {
		LogString("\t the bottom of the bag");
	    }
	}
	
	private void look() {
	    PrintRoom(true);
	}
	
	private void examine() {
	    if (inputActionData.Length > 0) {
		string item = inputActionData[0];
		bool inBackpack = false;
		
		foreach (Item i in backpack) 
		    if (i.name == item) {
			inBackpack = true;
			LogString("\n" + i.description);
		    }
		
		if (!inBackpack) {
		    LogString("\nYou don't have a " + item + " in your bag.");
		}
		
	    } else {
		LogString("\nexamine what?");
	    }
	}
	
	private void use() {
	    if (inputActionData.Length != 0) {
		string item = inputActionData[0];
		
		if (InInventory(item)) {
		    Room cur = columns[y].row[x];
		    
		    TextEvent[] reactions = cur.processAction(item);
		    
		    if (reactions != null) {
			foreach (TextEvent r in reactions) {
			    if (r.checkConditional) {
				switch (r.checkWhat) {
				    case TextEvent.EventConditionalType.INVENTORY:
					if (InInventory(r.checkFor))
					    UseHelper(r);
					else {
					    cur.addEvent(r);
					    LogString("\nYou can't use that here");
					}
					break;
				    case TextEvent.EventConditionalType.ROOM:
					if (Regex.IsMatch(cur.shortDescription + " " + cur.longDescription,
							  r.checkFor))
					    UseHelper(r);
					else {
					    cur.addEvent(r);
					    LogString("\nYou can't use that here");
					}
					break;
				}
			    } else {
				UseHelper(r);
			    }
			    
			    foreach (Item i in backpack) {
				if (i.name == item) {
				    backpack.Remove(i);
				    break;
				}
			    }
			}
			
		    } else {
			LogString("\nYou can't use that here");
		    }
		    
		} else {
		    LogString("\nYou don't have one of those!");
		}
		
	    } else {
		LogString("\nUse what?");
	    }
	    
	}
	
	private void clear() {
	    outputArea.text = "";
	    PrintRoom();
	}
	
	private void help() {
	    LogString("\nAvaliable commands are: ");
	    foreach (string s in actions) {
		LogString(s + " ");
	    }
	}
	
	
	[Serializable]
	public class RoomRow {
	    private string name = "Row";
	    
	    [SerializeField]
	    public static int numRooms;
	    
	    public Room[] row = new Room[numRooms];
	}
	
	[Serializable]
	public class Room {
	    public enum Direction {
		NORTH,
		SOUTH,
		EAST,
		WEST,
	    }
	    
	    private string name = "Room";
	    
	    public Direction[] connections;
	    public Direction[] lockedConnections;
	    public Item[] items;
	    public TextEvent[] events;
	    
	    public string shortDescription;
	    [TextArea] public string longDescription;
	    
	    public TextEvent[] processAction(string input) {
		List<TextEvent> processedEvents = new List<TextEvent>();
		foreach (TextEvent te in events)
		{
		    if (te.caused(input)) {
			events = events.Where((item, index) => item != te).ToArray();
			processedEvents.Add(te);
		    }
		}
		if (processedEvents.Count == 0)
		    return null;
		return processedEvents.ToArray();
	    }
	    
	    public void addEvent(TextEvent te) {
		Array.Resize(ref events, events.Length + 1);
		events[events.Length-1] = te;
	    }
	    
	    public void addEvents(TextEvent[] tes) {
		foreach (TextEvent te in tes)
		    addEvent(te);
	    }
	    
	    public void processEvent(TextEvent te) {
		string[] e = te.effectOptions;
		
		switch(te.effect) {
		    case TextEvent.EventEffect.UNLOCK:
			var d1 = (Direction) Enum.Parse(typeof(Room.Direction),
							e[0],
							true);
			
			Array.Resize(ref connections, connections.Length + 1);
			
			// checks if there is a locked door at that location
			if (isLockedDirection(e[0])) {
			    connections[connections.Length-1] = d1;
			} else {
			    throwError(UnlockError, e[0], te.roomCoords[0], te.roomCoords[1]);
			}
			lockedConnections = lockedConnections.Where((c, i) => c != d1).ToArray();
			break;
		    case TextEvent.EventEffect.ADD_ITEM:
			Item item = new Item();
			item.name = e[0];
			item.description = e[1];
			if (e.Length > 2) {
			    item.special = e[2];
			}
			Array.Resize(ref connections, items.Length + 1);
			items[items.Length-1] = item;
			break;
		    case TextEvent.EventEffect.CHANGE_DESCRIPTION:
			shortDescription = te.effectOptions[0];
			longDescription = te.effectOptions[1];
			break;
		    case TextEvent.EventEffect.LOCK:
			var d2 = (Direction) Enum.Parse(typeof(Room.Direction),
							e[0],
							true);
			
			Array.Resize(ref lockedConnections, lockedConnections.Length + 1);
			
			// checks if there is an unlocked door at that location
			if (validDirection(e[0])) {
			    lockedConnections[lockedConnections.Length-1] = d2;
			} else {
			    throwError(LockError, e[0], te.roomCoords[0], te.roomCoords[1]);
			}
			connections = connections.Where((c, i) => c != d2).ToArray();
			break;
		    case TextEvent.EventEffect.GAME_OVER:
			GAME_OVER = true;
			break;
		}
	    }
	    
	    public bool validDirection(Direction d) {
		return connections.Contains(d);
	    }
	    
	    public bool validDirection(string d) {
		return validDirection((Direction) Enum.Parse(typeof(Direction),
							     d,
							     true));
	    }
	    
	    public int itemInRoom(string i) {
		for (int j = 0; j < items.Length; j++)
		    if (items[j].name == i)
			return j;
		return -1;
	    }
	    
	    public bool isLockedDirection(Direction d) {
		return lockedConnections.Contains(d);
	    }
	    
	    public bool isLockedDirection(string d){
		return isLockedDirection((Direction) Enum.Parse(typeof(Direction),
								d,
								true));
	    }
	    
	    public void throwError(string text, string dir, int x, int y) {
		throw new FrameworkError(String.Format(text, dir, x, y));
	    }
	}
	
	[Serializable]
	public class TextEvent {
	    public enum EventEffect {
		UNLOCK,
		LOCK,
		ADD_ITEM,
		CHANGE_DESCRIPTION,
		GAME_OVER,
	    }
	    
	    public enum EventConditionalType {
		INVENTORY,
		ROOM,
	    }
	    
	    [SerializeField] private string cause;
	    [TextArea] [SerializeField] private string reaction;
	    public bool effectOtherRoom;
	    public bool checkConditional;
	    public EventConditionalType checkWhat;
	    public string checkFor;
	    public int[] roomCoords = new int[2];
	    public EventEffect effect;
	    [TextArea] public string[] effectOptions;
	    
	    public bool caused(string action) {
		return cause == action;
	    }
	    
	    public string getReaction() {
		return reaction;
	    }
	}
	
    }

    [Serializable]
    public class Item {
	public string name;
	public string description;
	public string special;
    }       

    public class FrameworkCommand : MonoBehaviour {
	public virtual void run(string[] inputData, List<Item> inventory) {
	    throw new CommandError(String.Format("unimplemented command: {0}",
						 this.name));
	}
	
	private class CommandError : FrameworkError {
	    public CommandError(string message) : base(message) {}
	}
    }
    
    public class FrameworkError : System.Exception {
	public FrameworkError(string message) : base(message) {}
    }
}
