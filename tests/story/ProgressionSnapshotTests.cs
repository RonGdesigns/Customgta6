using System;
using System.IO;
using System.Linq;
using System.Text;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions;

public static partial class StoryTests
{
 static void ProgressionSnapshot()
 {
  var text=new StringBuilder();
  text.AppendLine("| Milestone | Ice | Gohan | Guess |");text.AppendLine("|---|---|---|---|");
  foreach(string id in WeaponProgression.RewardMissions)
   text.AppendLine("| "+id+" | "+string.Join(" | ",Protagonist.All.Select(h=>WeaponProgression.ReceivesReward(id,h.Slot)?WeaponProgression.NameOf(WeaponProgression.Rewards(id)[(int)h.Slot]):"—"))+" |");
  text.AppendLine();text.AppendLine("| Job | First-completion cash |");text.AppendLine("|---|---:|");
  var state=CampaignState.Load(Path.Combine(root,"progression-doc.json"));var catalog=new MissionCatalog();
  foreach(string id in Enumerable.Range(1,43).Select(i=>"M"+i.ToString("00")).Concat(Enumerable.Range(1,6).Select(i=>"SM"+i.ToString("00"))))
  {catalog.All.Add(Def(id,id.StartsWith("SM")?"solo":"main"));int before=state.CashOnHand;state.MarkComplete(id,catalog);text.AppendLine("| "+id+" | $"+(state.CashOnHand-before).ToString("N0",System.Globalization.CultureInfo.InvariantCulture)+" |");}
  text.AppendLine();text.AppendLine("Total first-completion cash: **$"+state.CashOnHand.ToString("N0",System.Globalization.CultureInfo.InvariantCulture)+"**. M19–M21 pay as part of the M22 heist completion. Replays do not pay again.");
  string generated=text.ToString().Replace("\r\n","\n");
  File.WriteAllText(Path.Combine(Repo,"build","progression-runtime.md"),generated,new UTF8Encoding(false));
  string doc=File.ReadAllText(Path.Combine(Repo,"docs","PROGRESSION-GUIDE.md")).Replace("\r\n","\n");
  // A source-backed table, not a second payout formula in a document generator.
  Check(doc.Contains(generated.Trim()),"The progression guide matches every current runtime payout and weapon reward");
 }
}
