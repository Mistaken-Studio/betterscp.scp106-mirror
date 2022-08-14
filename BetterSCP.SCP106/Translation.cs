// -----------------------------------------------------------------------
// <copyright file="Translation.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Exiled.API.Interfaces;

namespace Mistaken.BetterSCP.SCP106
{
    internal class Translation : ITranslation
    {
        public string StartMessage { get; set; } = "<color=red><b><size=500%>UWAGA</size></b></color><br><br><br><br><br><br><size=90%>Rozgrywka jako <color=red>SCP 106</color> na tym serwerze jest zmodyfikowana, <color=red>SCP 106</color> po użyciu przycisku do <color=yellow>tworzenia portalu</color> przeniesie się do <color=yellow>losowego</color> pomieszczenia w <color=yellow><b>innej</b></color> strefie niż ta w której obecnie się znajduje<br><br>Przycisk do <color=yellow>użycia portalu</color> działa tak samo tylko że przenosi do <color=yellow>losowego</color> pomieszczenia w <color=yellow><b>tej samej</b></color> strefie w jakiej znajduje się <color=red>SCP 106</color></size>";

        public string UnluckyMessage { get; set; } = "You've picked the wrong exit fool!";

        public string DoorDenyMessage { get; set; } = "<size=150%>In order to <color=yellow>open</color> this door at least <color=yellow>2</color> generators need to be <color=yellow>engaged</color></size>";
    }
}
