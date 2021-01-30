using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class NameGenerator
{
    private static readonly string[] Syllables =
    {
        "as", "pi", "di", "ske", "bell", "at", "rix", "moth", "al", "lah", "ka", "veh", "fle", "ge", "ton", "te",
        "to", "jil", "co", "cib", "ol", "ca", "kra", "z", "pher", "kad", "mul", "i", "phe", "in", "ha", "dar", "k",
        "ang", "tyl", "ax", "ól", "otl", "da", "lim", "sad", "r", "go", "me", "isa", "hae", "dus", "vel", "bar",
        "nard", "star", "lau", "si", "na", "sh", "par", "um", "leo", "cel", "ae", "no", "mp", "ro", "ta", "nev",
        "hel", "ve", "ti", "os", "cor", "car", "oli", "cha", "son", "ash", "le", "sha", "bha", "ra", "ni", "za",
        "ah", "koe", "ia", "de", "ne", "bo", "la", "zu", "ben", "el", "bi", "jat", "uan", "po", "ris", "aus", "tra",
        "lis", "u", "ruk", "wan", "nia", "bor", "ea", "nen", "que", "mou", "houn", "ru", "ch", "bah", "sai", "ph",
        "mar", "fik", "mus", "ica", "che", "leb", "bu", "bup", "mek", "buda", "pri", "ma", "hya", "dum", "an",
        "res", "ba", "bib", "hā", "hor", "hoe", "tis", "lit", "ako", "raka", "stri", "meb", "suta", "muth", "alle",
        "th", "azel", "fa", "fage", "bat", "en", "kai", "tos", "hama", "ter", "crus", "aster", "ope", "ste", "rope",
        "rana", "prox", "ima", "cen", "tauri", "sche", "nus", "akan", "hun", "or", "men", "kar", "kor", "neph",
        "oros", "dom", "bay", "les", "ath", "piau", "unur", "gun", "ite", "bos", "ona", "zube", "nel", "gen", "ubi",
        "thu", "ban", "graff", "ias", "nath", "tia", "ki", "maz", "aal", "ai", "zau", "rak", "cur", "sa", "alm",
        "elik", "rosa", "lía", "deca", "stro", "aye", "yar", "wady", "dia", "mira", "las", "lus", "kit", "alpha",
        "ran", "ellus", "tert", "ius", "cit", "alá", "sir", "kaa", "mac", "ondo", "vega", "sula", "fat", "sada",
        "suud", "mor", "iah", "musc", "ida", "alf", "ecca", "mer", "id", "iana", "auva", "sch", "eat", "ner", "via",
        "mur", "zim", "min", "dem", "pol", "aris", "ras", "ela", "sed", "alha", "gue", "cano", "pus", "sar", "gas",
        "yed", "fe", "lix", "var", "duhr", "it", "onda", "ati", "uh", "cher", "tan", "wazn", "sac", "lat", "eni",
        "kham", "bal", "zub", "ak", "ribi", "itân", "arc", "turus", "wur", "ren", "ar", "ich", "sal", "gethi", "ri",
        "gel", "post", "er", "ior", "asel", "primus", "kuma", "lux", "ani", "ara", "uk", "lun", "kali", "nan",
        "keb", "realis", "fr", "anz", "liber", "tas", "gat", "ria", "re", "gor", "dschu", "gulus", "rasta", "tur",
        "eis", "mir", "am", "bik", "rai", "funi", "salm", "neb", "su", "bra", "tali", "tha", "unuk", "hai", "asym",
        "ula", "benet", "kaff", "alji", "dhma", "shel", "iak", "kab", "tai", "yi", "se", "cundus", "gia", "usar",
        "mi", "sam", "nal", "edi", "ham", "fak", "ler", "zi", "toli", "man", "quill", "ebla", "rasa", "gin", "pha",
        "sol", "heka", "skat", "illyr", "ian", "fu", "rud", "ceb", "alrai", "nem", "bus", "eddi", "polis", "pipi",
        "rima", "ark", "ab", "prior", "rami", "bélé", "nos", "gra", "hass", "aleh", "sua", "locin", "nun", "tejat",
        "fafn", "ir", "zos", "asc", "ella", "diph", "diya", "por", "segi", "rir", "ion", "koch", "ahpú", "lion",
        "rock", "tim", "mia", "placi", "prae", "cipua", "elga", "far", "okab", "dhen", "eb", "hal", "fang", "kur",
        "hah", "tape", "cue", "nata", "zavi", "java", "ser", "felis", "dub", "he", "chara", "san", "suna", "fomal",
        "haut", "deneb", "gedi", "ikli", "chium", "pet", "mine", "lauva", "bet", "fum", "sama", "kah", "pai", "kau",
        "hale", "muph", "rid", "eto", "tula", "lilii", "borea", "ga", "crux", "luc", "ilin", "bur", "huc", "maha",
        "sim", "ava", "spi", "hog", "gar", "alku", "rah", "aza", "leh", "nyam", "ien", "bee", "mim", "lima", "suh",
        "ail", "nyi", "kas", "okul", "arin", "beid", "xam", "idim", "ura", "tab", "glo", "ask", "gi", "enah", "gir",
        "av", "sām", "aya", "gak", "yid", "nush", "agak", "xu", "ange", "keid", "ectra", "giedi", "fumal", "kent",
        "jish", "ui", "enif", "meri", "diana", "navi", "tupi", "ster", "nacht", "heze", "bote", "thee", "yil",
        "dun", "elt", "anin", "chao", "phraya", "dno", "ces", "irena", "poltr", "form", "osa", "imai", "sagar",
        "matha", "mera", "wasa", "azha", "poer", "mah", "sati", "sic", "grez", "armus", "arkab", "kaus", "media",
        "tere", "bellum", "atik", "tay", "geta", "iibuu", "muy", "ogma", "fuyue", "real", "is", "vin", "iatrix",
        "chir", "syrma", "alís", "chamu", "kuy", "she", "ratan", "zuben", "akrab", "tupã", "mel", "eph", "toni",
        "grum", "ium", "lara", "wag", "axa", "eda", "sich", "nekk", "bih", "tar", "meis", "jabb", "nash", "ira",
        "cer", "vantes", "emiw", "cast", "homam", "phoen", "icia", "hat", "ysa", "phec", "gum", "ala", "arne",
        "faw", "nesch", "ali", "az", "midi", "alba", "elku", "moldo", "veanu", "ple", "ione", "nair", "saif",
        "belel", "yang", "shou", "scep", "trum", "mal", "mok", "cei", "rigil", "aurus", "taik", "pea", "kib",
        "dulf", "im", "dzi", "emali", "dingo", "lay", "xihe", "we", "zen", "mon", "tuno", "net", "pin", "coya",
        "bit", "cap", "taka", "ach", "bere", "hynia", "sec", "unda", "sham", "mago", "mizar", "kek", "ouan", "kak",
        "tarf", "gonea", "fulu", "mön", "tui", "dì", "wö", "naos", "zhang", "gu", "dja", "torc", "ular", "chia",
        "zam", "nika", "wiy", "mes", "arth", "betel", "geuse", "bun", "athe", "byne", "chbia", "lich", "reva",
        "kabd", "hili", "sika", "do", "fida", "izar", "lie", "sma", "ko", "cita", "delle", "pro", "cyon", "nahn",
        "super", "má", "rohu", "buna", "sin", "istra", "coper", "nicus", "teg", "shar", "jah", "pel", "heim",
        "tita", "win", "veri", "tate", "caph", "tara", "zed", "chow", "cu", "jam", "rijl", "awwa", "aust", "ralis",
        "dah", "nás", "baek", "du",
    };

    private static readonly (char ch, float weight)[] Seperators =
    {
        (' ', 1f), 
        ('-', 0.02f)
    };

    private static readonly (int num, float weight)[] syllableCountChances =
    {
        (1, 1.0f),
        (2, 3.0f),
        (3, 2.0f),
        (4, 1.0f),
        (5, 0.5f),
        (6, 0.1f),
    };
    
    public static string GenerateName(RandomX rng)
    {
        string result = string.Empty;

        int length = (int) Mathf.Lerp(2f, 15f, rng.RandomGaussianSlice(0f, 1f, -0.5f, 1f)); 
        // rng.Range(2, 15);
        int parts = (int) rng.Range(1f, Mathf.Max(1f, Mathf.Sqrt(length * 0.75f)));
        // Each part has at least one letter
        length -= parts;
        int[] lenOfEachPart = Enumerable.Repeat(1, parts).ToArray();
        
        // Assign the remaining syllables to parts
        for (int i = 0; i < length; i++)
        {
            lenOfEachPart[rng.Range(0, lenOfEachPart.Length)]++;
        }
        
        for (int i = 0; i < lenOfEachPart.Length; i++)
        {
            if (i != 0)
            {
                result += Seperators.SelectWeighted(rng.value, c => c.weight).ch;
            }
            int len = lenOfEachPart[i];
            string part = string.Empty;
            while (part.Length < len)
            {
                string syllable = Syllables.SelectRandom();
                // Capitalise first letter
                if (part.Length == 0)
                {
                    syllable = Char.ToUpper(syllable.First()) + syllable.Substring(1);
                }
                part += syllable;
            }

            result += part;
        }
        
        return result;
    }

    public class UniqueNameGenerator
    {
        private readonly HashSet<string> usedNames = new HashSet<string>();
        private readonly RandomX rng;

        public UniqueNameGenerator(RandomX rng = null)
        {
            this.rng = rng ?? new RandomX();
        }
        
        public string Next()
        {
            int size = this.usedNames.Count;
            string newName = string.Empty;
            while (this.usedNames.Count == size)
            {
                newName = GenerateName(this.rng);
                this.usedNames.Add(newName);
            }

            return newName;
        }
    }

    // public static string GenerateName(RandomX rng)
    // {
    //     string result = string.Empty;
    //
    //     int totalSyllables = syllableCountChances.SelectWeighted(rng.value, f => f.weight).num;
    //     int parts = (int) rng.Range(1f, Mathf.Sqrt(totalSyllables));
    //     // Each part has at least one syllable so we can assign that 1 immediately
    //     totalSyllables -= parts;
    //     int[] syllablesInEachPart = Enumerable.Repeat(1, parts).ToArray();
    //     // Assign the remaining syllables to parts
    //     for (int i = 0; i < totalSyllables; i++)
    //     {
    //         syllablesInEachPart[rng.Range(0, parts)]++;
    //     }
    //     
    //     for (int i = 0; i < syllablesInEachPart.Length; i++)
    //     {
    //         if (i != 0)
    //         {
    //             result += Seperators.SelectWeighted(rng.value, c => c.weight).ch;
    //         }
    //         int syllables = syllablesInEachPart[i];
    //         for (int j = 0; j < syllables; j++)
    //         {
    //             string syllable = Syllables.SelectRandom();
    //             // Capitalise first letter
    //             if (j == 0)
    //             {
    //                 syllable = Char.ToUpper(syllable.First()) + syllable.Substring(1);
    //             }
    //             result += syllable;
    //         }
    //     }
    //     
    //     return result;
    // }
    
    // public static string GenerateName(int minParts, int maxParts, int minSyllables, int maxSyllables, RandomX rng)
    // {
    //     string result = string.Empty;
    //     
    //     int parts = rng.Range(minParts, maxParts);
    //     for (int i = 0; i < parts; i++)
    //     {
    //         if (i != 0)
    //         {
    //             result += Seperators.SelectWeighted(rng.value, c => c.weight).ch;
    //         }
    //         int syllables = rng.Range(minSyllables, maxSyllables);
    //         for (int j = 0; j < syllables; j++)
    //         {
    //             var syllable = Syllables.SelectRandom();
    //             // Capitalise first letter
    //             if (j == 0)
    //             {
    //                 syllable = Char.ToUpper(syllable.First()) + syllable.Substring(1);
    //             }
    //             result += syllable;
    //         }
    //     }
    //     
    //     return result;
    // }
}