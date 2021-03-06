﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace PKHeX
{
    public partial class LegalityAnalysis
    {
        private readonly PK6 pk6;
        private object EncounterMatch;
        private List<WC6> CardMatch;
        private Type EncounterType;
        private LegalityCheck ECPID, Nickname, IDs, IVs, EVs, Encounter, Level, Ribbons, Ability, Ball, History, OTMemory, HTMemory, Region, Form, Misc;
        private LegalityCheck[] Checks => new[] { Encounter, Level, Form, Ball, Ability, Ribbons, ECPID, Nickname, IVs, EVs, IDs, History, OTMemory, HTMemory, Region, Misc };

        public bool Valid = true;
        public bool SecondaryChecked;
        public int[] RelearnBase;
        public LegalityCheck[] vMoves = new LegalityCheck[4];
        public LegalityCheck[] vRelearn = new LegalityCheck[4];
        public string Report => getLegalityReport();
        public string VerboseReport => getVerboseLegalityReport();

        public LegalityAnalysis(PK6 pk)
        {
            pk6 = pk;
            updateRelearnLegality();
            updateMoveLegality();
            updateChecks();
            getLegalityReport();
        }

        public void updateRelearnLegality()
        {
            try { vRelearn = verifyRelearn(); }
            catch { for (int i = 0; i < 4; i++) vRelearn[i] = new LegalityCheck(Severity.Invalid, "Internal error."); }
            SecondaryChecked = false;
        }
        public void updateMoveLegality()
        {
            try { vMoves = verifyMoves(); }
            catch { for (int i = 0; i < 4; i++) vMoves[i] = new LegalityCheck(Severity.Invalid, "Internal error."); }
            SecondaryChecked = false;
        }

        private void updateChecks()
        {
            Encounter = verifyEncounter();
            EncounterType = EncounterMatch?.GetType();
            ECPID = verifyECPID();
            Nickname = verifyNickname();
            IDs = verifyID();
            IVs = verifyIVs();
            EVs = verifyEVs();
            Level = verifyLevel();
            Ribbons = verifyRibbons();
            Ability = verifyAbility();
            Ball = verifyBall();
            History = verifyHistory();
            OTMemory = verifyOTMemory();
            HTMemory = verifyHTMemory();
            Region = verifyRegion();
            Form = verifyForm();
            Misc = verifyMisc();
            SecondaryChecked = true;
        }
        private string getLegalityReport()
        {
            if (!pk6.Gen6)
                return "Analysis only available for Pokémon that originate from X/Y & OR/AS.";
            
            var chks = Checks;

            string r = "";
            for (int i = 0; i < 4; i++)
                if (!vMoves[i].Valid)
                    r += $"{vMoves[i].Judgement} Move {i + 1}: {vMoves[i].Comment}" + Environment.NewLine;
            for (int i = 0; i < 4; i++)
                if (!vRelearn[i].Valid)
                    r += $"{vRelearn[i].Judgement} Relearn Move {i + 1}: {vRelearn[i].Comment}" + Environment.NewLine;

            if (r.Length == 0 && chks.All(chk => chk.Valid))
                return "Legal!";

            Valid = false;
            // Build result string...
            r += chks.Where(chk => !chk.Valid).Aggregate("", (current, chk) => current + $"{chk.Judgement}: {chk.Comment}{Environment.NewLine}");

            return r.TrimEnd();
        }
        private string getVerboseLegalityReport()
        {
            string r = getLegalityReport() + Environment.NewLine;
            r += "===" + Environment.NewLine + Environment.NewLine;
            int rl = r.Length;

            for (int i = 0; i < 4; i++)
                if (vMoves[i].Valid)
                    r += $"{vMoves[i].Judgement} Move {i + 1}: {vMoves[i].Comment}" + Environment.NewLine;
            for (int i = 0; i < 4; i++)
                if (vRelearn[i].Valid)
                    r += $"{vRelearn[i].Judgement} Relearn Move {i + 1}: {vRelearn[i].Comment}" + Environment.NewLine;

            if (rl != r.Length) // move info added, break for next section
                r += Environment.NewLine;

            var chks = Checks;
            r += chks.Where(chk => chk.Valid && chk.Comment != "Valid").OrderBy(chk => chk.Judgement) // Fishy sorted to top
                .Aggregate("", (current, chk) => current + $"{chk.Judgement}: {chk.Comment}{Environment.NewLine}");
            return r.TrimEnd();
        }

        public int[] getSuggestedRelearn()
        {
            if (RelearnBase == null)
                return new int[4];

            if (!pk6.WasEgg)
                return RelearnBase;

            List<int> window = new List<int>(RelearnBase);

            for (int i = 0; i < 4; i++)
                if (!vMoves[i].Valid || vMoves[i].Flag)
                    window.Add(pk6.Moves[i]);

            if (window.Count < 4)
                window.AddRange(new int[4 - window.Count]);
            return window.Skip(window.Count - 4).Take(4).ToArray();
        }
    }
}
