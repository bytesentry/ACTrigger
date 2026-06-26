using System.Collections.Generic;
using System;

namespace ACTrigger.UI.Models;

public class DebuffGroup
{
    public string TargetName { get; set; } = "";

    public int TargetId { get; set; }

    public List<TrackedDebuff> Debuffs { get; set; } = [];

}