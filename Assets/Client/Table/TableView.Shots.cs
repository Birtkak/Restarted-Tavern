using System.Linq;
using RestartedTavern.Rules;

namespace RestartedTavern.Client.Table
{
    /// <summary>
    /// Command-line scene setup for screenshots (the Tavern Guide's pictures, Tools/GuideShots): put chosen cards on the
    /// battlefield, set Gold, take one action, stage the combat, hide the tutorial's coach box.
    /// </summary>
    public sealed partial class TableView
    {
        /// <summary>-place / -placeopp: comma-separated card ids. "id*2" has 2 damage, "id!" is tapped.</summary>
        private string _place, _placeOpp;
        private int _goldShot = -1;
        private string _actShot;
        private bool _stageShot, _noCoach, _settleShot, _shotHover;

        /// <summary>
        /// Puts the -place cards onto P1's battlefield and the -placeopp cards onto P2's. An Equipment goes on the last
        /// creature placed on its side; a Curse on the last creature placed on the other side.
        /// </summary>
        private void PlaceShotCards()
        {
            var st = _s.State;
            CardInstance lastMine = null, lastTheirs = null;
            for (int seat = 0; seat < 2; seat++)
            {
                string list = seat == 0 ? _place : _placeOpp;
                if (string.IsNullOrEmpty(list)) continue;
                var p = st.Players[seat];
                foreach (var raw in list.Split(','))
                {
                    string id = raw.Trim();
                    bool tapped = id.EndsWith("!");
                    id = id.TrimEnd('!');
                    int damage = 0;
                    int star = id.IndexOf('*');
                    if (star > 0) { int.TryParse(id.Substring(star + 1), out damage); id = id.Substring(0, star); }
                    var def = _s.Engine.Cards.All.FirstOrDefault(d => d.Id == id);
                    if (def == null) continue;
                    var c = new CardInstance
                    {
                        Id = new ObjectId(st.NextObjectId++), DefinitionId = def.Id, Owner = p.Id, Controller = p.Id,
                        Zone = Zone.Battlefield, Timestamp = st.NextTimestamp++, Tapped = tapped, Damage = damage,
                    };
                    var own = seat == 0 ? lastMine : lastTheirs;
                    var other = seat == 0 ? lastTheirs : lastMine;
                    if (def.Type == CardType.Equipment && own != null) c.AttachedToObject = own.Id;
                    if (def.Type == CardType.Curse && other != null) c.AttachedToObject = other.Id;
                    if (def.IsCreature) { if (seat == 0) lastMine = c; else lastTheirs = c; }
                    p.Battlefield.Add(c);
                }
            }
            if (_goldShot >= 0)
                foreach (var p in st.Players) p.Gold = _goldShot;
        }

        /// <summary>-act id: the viewer takes their first legal action with that card (a targeted one if there is one). -stage: stage the combat.</summary>
        private void ApplyShotActions()
        {
            // After the tutorial set its game up (it starts a new one).
            if (_place != null || _placeOpp != null || _goldShot >= 0) { PlaceShotCards(); OnSessionChanged(); }
            // -settle: the bot acts until the viewer has to decide something.
            if (_settleShot)
            {
                for (int i = 0; i < 40 && !_s.State.IsGameOver; i++)
                {
                    if (_s.BotToAct) _s.StepBot();
                    else break;
                }
                if (_s.HandoffPending) _s.AcknowledgeHandoff();
                _pendingEvents.Clear();
                OnSessionChanged();
            }
            if (_actShot != null && _s.HumanToAct)
            {
                var actions = _s.LegalForViewer().Where(a => !a.Card.IsNone && _s.State.FindObject(a.Card)?.DefinitionId == _actShot).ToList();
                var pick = actions.FirstOrDefault(a => a.Targets != null && a.Targets.Length > 0) ?? actions.FirstOrDefault();
                if (pick != null)
                {
                    _s.Submit(pick);
                    _pendingEvents.Clear();
                    OnSessionChanged();
                }
            }
            if (_stageShot && _stage != null)
            {
                foreach (var id in _stage.Candidates().ToList())
                {
                    if (_stage.IsBlocking) { var b = _stage.BlockableBy(id); if (b.Count > 0) _stage.StageBlocker(id, b[0]); }
                    else _stage.StageAttacker(id);
                }
                _dirty = true;
            }
        }
    }
}
