using System.Collections.Generic;
using RestartedTavern.Client.Logic;
using RestartedTavern.Rules;
using UnityEngine;

namespace RestartedTavern.Client.Table
{
    /// <summary>
    /// Sound effects (user, 2026-10-10): every kind of card played, abilities, Powers, triggers and passing, each kind
    /// in its faction's voice (<see cref="SfxSynth"/>, synthesized once per kind and faction, no audio files). Sounds are
    /// scheduled with the animation beats, so they land on the moment they belong to. Volume is a setting.
    /// </summary>
    public sealed partial class TableView
    {
        private readonly Dictionary<(SfxKind, string), AudioClip> _clips = new Dictionary<(SfxKind, string), AudioClip>();
        private readonly List<AudioSource> _voices = new List<AudioSource>();
        private float _volume = 0.6f;
        private bool _passSound = true, _triggerSound = true, _buttonSound = true;
        private float _lastHoverSound;
        private static readonly float[] Volumes = { 0f, 0.3f, 0.6f, 1f };

        private void SetupAudio()
        {
            if (FindAnyObjectByType<AudioListener>() == null) gameObject.AddComponent<AudioListener>();
            for (int i = 0; i < 12; i++)
            {
                var src = gameObject.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.spatialBlend = 0f;
                _voices.Add(src);
            }
            AudioListener.volume = _volume;
            Ui.ButtonSound = OnButtonSound;
        }

        /// <summary>Buttons tick on hover and tap on click (playtest 2026-10-10_173612); a sweep over a row ticks once.</summary>
        private void OnButtonSound(bool hover)
        {
            if (!_buttonSound) return;
            if (hover)
            {
                if (Time.unscaledTime - _lastHoverSound < 0.06f) return;
                _lastHoverSound = Time.unscaledTime;
                PlaySfx(SfxKind.UiHover, null, 0f, 0.35f);
            }
            else PlaySfx(SfxKind.UiClick, null, 0f, 0.6f);
        }

        private void SetVolume(float v)
        {
            _volume = v;
            AudioListener.volume = v;
            PlayerPrefs.SetFloat("volume", v);
        }

        /// <summary>Plays a sound after <paramref name="delay"/> seconds (in the faction's voice; null = neutral).</summary>
        private void PlaySfx(SfxKind kind, string faction, float delay = 0f, float volume = 1f)
        {
            if (_autoshot != null || _volume <= 0f || _voices.Count == 0) return;
            faction = faction ?? "neutral";
            if (!_clips.TryGetValue((kind, faction), out var clip))
            {
                var samples = SfxSynth.Render(kind, faction);
                clip = AudioClip.Create(kind + "_" + faction, samples.Length, 1, SfxSynth.SampleRate, false);
                clip.SetData(samples, 0);
                _clips[(kind, faction)] = clip;
            }
            AudioSource free = null;
            foreach (var v in _voices)
                if (!v.isPlaying) { free = v; break; }
            if (free == null) return; // a pile-up: drop it rather than cut another sound off
            free.clip = clip;
            free.volume = volume;
            free.pitch = 1f;
            free.PlayDelayed(Mathf.Max(0f, delay));
        }

        /// <summary>A card's faction for its sound; a Tavern Dweller speaks for its first faction.</summary>
        private string FactionOf(string definitionId)
        {
            if (definitionId == null || !_db.Contains(definitionId)) return "neutral";
            var def = _db.Get(definitionId);
            if (def.Type == CardType.TavernDweller && def.TavernDwellerFactions.Length > 0) return def.TavernDwellerFactions[0];
            return def.Faction;
        }

        private static SfxKind KindOf(CardDefinition def)
        {
            switch (def.Type)
            {
                case CardType.Instant: return SfxKind.Instant;
                case CardType.Sorcery: return SfxKind.Sorcery;
                case CardType.Equipment: return SfxKind.Equipment;
                case CardType.Relic: return SfxKind.Relic;
                case CardType.Curse: return SfxKind.Curse;
                default: return SfxKind.Creature;
            }
        }

        // One attack / block / death sound per batch of events (a whole attack is one war drum, not one per attacker).
        private int _attackSoundBatch = -1, _blockSoundBatch = -1, _deathSoundBatch = -1, _soundBatch;

        /// <summary>Called by PlayBeats before each batch of events.</summary>
        private void NextSoundBatch() => _soundBatch++;

        /// <summary>The sound for one event, at its moment in the beat timeline (called from PlayBeats). LoR's events.</summary>
        private void SoundFor(GameEvent e, float t)
        {
            switch (e)
            {
                case AttackerDeclaredEvent _ when _attackSoundBatch != _soundBatch:
                    _attackSoundBatch = _soundBatch;
                    PlaySfx(SfxKind.Attack, null, t);
                    break;
                case BlockerDeclaredEvent _ when _blockSoundBatch != _soundBatch:
                    _blockSoundBatch = _soundBatch;
                    PlaySfx(SfxKind.Block, null, t);
                    break;
                case CreatureDiedEvent d when _deathSoundBatch != _soundBatch:
                    _deathSoundBatch = _soundBatch;
                    PlaySfx(SfxKind.Death, FactionOf(d.DefinitionId), t, 0.8f);
                    break;
                case SpellCastEvent s when _db.Contains(s.DefinitionId):
                    PlaySfx(KindOf(_db.Get(s.DefinitionId)), FactionOf(s.DefinitionId), t);
                    break;
                case AbilityActivatedEvent a:
                    PlaySfx(a.IsTavernDwellerPower ? SfxKind.Power : SfxKind.Ability, FactionOf(a.SourceDefinitionId), t);
                    break;
                case AttachedEvent at:
                    PlaySfx(SfxKind.Equip, FactionOf(at.EquipmentDefinitionId), t);
                    break;
                case AbilityTriggeredEvent tr when _triggerSound:
                    PlaySfx(SfxKind.Trigger, FactionOf(tr.SourceDefinitionId), t, 0.7f);
                    break;
                case CounteredEvent _:
                    PlaySfx(SfxKind.Countered, null, t);
                    break;
                case PriorityPassedEvent p when _passSound && !p.Automatic && p.ChainCount == 0 && p.Step == Step.Main1:
                    PlaySfx(SfxKind.Pass, null, t, p.Player == _snap.Viewer ? 1f : 0.8f);
                    break;
                case RoundStartedEvent _:
                    PlaySfx(SfxKind.RoundStart, null, t, 0.7f);
                    break;
            }
        }
    }
}
