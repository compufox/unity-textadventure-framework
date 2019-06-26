using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using AdventureFramework;

public class watch : FrameworkCommand {
    public override void run(string[] inputData, List<Item> inventory) {
	if (Framework.InInventory(inputData[0])) {
	    Framework.LogString(String.Format("You watch {0}.....but nothing happens",
					      inputData[0]));
	} else {
	    Framework.LogString("You don't seem to have one of those...");
	}
    }
}
