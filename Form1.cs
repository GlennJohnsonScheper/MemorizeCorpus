using System.Collections.Immutable;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using static System.Windows.Forms.LinkLabel;

/*
 * https://github.com/GlennJohnsonScheper/MemorizeCorpus
 * This program is in the public domain as per UNLICENSE.
 * 
 * The three files Form1.cs, engwebu.cs, and kjvbible.cs
 * can build a WinForm application in Visual Studio 2022.
 * 
 * The Windows Desktop app helps a user to memorize
 * and ponder the public domain World English Bible,
 * and the public domain King James Bible, included.
 *
 * To re-build this application,
 * Create a new WinForm project.
 * Double-click the visual form.
 * 
 * Change all form1.cs content to this very text.
 * Add engwebu.cs and kjvbible.cs to the project.
 * Set Build Mode to Release, Build and Enjoy it.
 */

namespace MemorizeCorpus
{
    public partial class Form1 : Form
    {
        // Visual Studio boilerplate procedure:
        public Form1()
        {
            InitializeComponent();
        }

        static string FormCaptionPrefix = "MemorizeCorpus";

        private TextBox tb = new TextBox();

        const int TEXTBOX_PADDING = 10;

        // How many display chars?
        // Empirical on my laptop,
        // Win 11 res 1920 x 1080.
        // Hardship to solve well.
        const int TEXTBOX_CAPACITY = 600;

        // Hurt about the middle 1/3 of view
        const int DISTRESS_LENGTH = TEXTBOX_CAPACITY / 3;

        // How much to randomly advance view location?
        const int TYPICAL_ADVANCE = TEXTBOX_CAPACITY * 3;

        // Let user select either WEB or KJV corpus at start:
        // E.g., for 'w', you get:
        static string corpusActive = EBibleOrg.EngWebU.corpus;
        static string corpusTitle = "World English Bible";
        static string corpusTrigraph = "WEB";

        // first of three program phases

        static bool inWelcomeScreen = true;

        static string WelcomeScreenInstructions = """
            MemorizeCorpus program is in the Public Domain.
            github.com/GlennJohnsonScheper/MemorizeCorpus

            User types in missing letters where _ appears.
            At any time, press ESCAPE key to exit program.

            Please press either k or w now to set a bible:

            k = "King James Version" @ Gutenberg.org.

            w = "World English Bible Updated" @ EBible.org.
            """;

        // second of three program phases

        static bool inBookIndexScreen = true;

        static string BookIndexScreenPrefix = """
            At any time, you may...

            Type missing _ letters to advance,
            Type SPACE to reveal one _ letter,

            Type < to go back in book slightly,
            Type > to advance in book slightly,
            Type ? to jump in book at random.

            type 3 digits to set current book,
            Type / to set another random book.
            
            Type / now to get started...


            """;

        // The third program phase will be when user will be tested...


        // The following technique helps to minimize display flicker.

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, Int32 wMsg, bool wParam, Int32 lParam);
        private const int WM_SETREDRAW = 11;
        private void Redraw_Textbox(string newContents, int selectionStart)
        {
            // this helps to prevent display flicker
            SendMessage(tb.Handle, WM_SETREDRAW, false, 0);
            Thread.Sleep(15);
            tb.Text = newContents;
            tb.SelectionStart = selectionStart;
            tb.SelectionLength = (selectionStart > 0 ? 1 : 0);
            Thread.Sleep(15);
            SendMessage(tb.Handle, WM_SETREDRAW, true, 0);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // form1 was created by Visual Studio
            this.WindowState = FormWindowState.Maximized;
            this.Text = FormCaptionPrefix;
            // (0,255,0) monochrome LIME color
            // kills eyeglass refraction rainbows
            this.BackColor = Color.Lime;
            this.SizeChanged += Form1_SizeChanged;

            // textbox was created above
            tb.Multiline = true;
            tb.Top = TEXTBOX_PADDING;
            tb.Left = TEXTBOX_PADDING;
            tb.Width = this.ClientSize.Width - 2 * TEXTBOX_PADDING;
            tb.Height = this.ClientSize.Height - 2 * TEXTBOX_PADDING;
            tb.BorderStyle = BorderStyle.None;
            tb.BackColor = Color.Lime;
            tb.Font = new Font("Lucida Sans Typewriter", 38);
            tb.KeyPress += Tb_KeyPress;
            this.Controls.Add(tb);

            // Here we go!
            Redraw_Textbox(WelcomeScreenInstructions, 0);
        }

        private void Form1_SizeChanged(object? sender, EventArgs e)
        {
            // An 11-th hour addition. Does not know/set selection.
            tb.Width = this.ClientSize.Width - 2 * TEXTBOX_PADDING;
            tb.Height = this.ClientSize.Height - 2 * TEXTBOX_PADDING;
            tb.Invalidate();
        }

        private void Tb_KeyPress(object? sender, KeyPressEventArgs e)
        {
            // Handles case-sensitive characters.
            // Navigation keys operate elsewhere.

            char c = e.KeyChar;
            if (c == (char)Keys.Escape)
            {
                /* Coding diagnostics guide me.
                TallyWordLengthEffect();
                TallyDocFreqEffect();
                TallyTermFreqEffect();
                */

                Application.Exit();
            }
            e.Handled = true;

            if (inWelcomeScreen)
            {
                // Phase 1

                // Await only k or w keypress:
                switch (c)
                {
                    case 'k':
                    case 'K':
                        corpusActive = GutenbergOrg.KJVBible.corpus;
                        corpusTitle = "King James Bible";
                        corpusTrigraph = "KJV";
                        inWelcomeScreen = false;
                        break;
                    case 'w':
                    case 'W':
                        corpusActive = EBibleOrg.EngWebU.corpus;
                        corpusTitle = "World English Bible";
                        corpusTrigraph = "WEB";
                        inWelcomeScreen = false;
                        break;
                }
                if (inWelcomeScreen == false)
                {
                    PrepareCorpusIndexScreen();
                    PrepareCorpusMeasurements();
                }
            }
            else if (inBookIndexScreen)
            {
                // Phase 2

                // Allow only a book-choosing keypress:
                if (char.IsDigit(c) || c == '/')
                {
                    Phase3KeyPressParadigm(c);
                }
                else
                {
                    accumulate3DigitStr = "";
                }
            }
            else
            {
                // Phase 3

                /* Glenn's secret testing loop.
                 * 
                if (c == '$')
                {
                    for (int i = 1; i < 60; i++)
                    {
                        // try which movement chars?

                        // random choice of new book.
                        Phase3KeyPressParadigm('/');
                        tb.Invalidate();
                        Thread.Sleep(1000);

                        // random slight regress.
                        Phase3KeyPressParadigm('<');
                        tb.Invalidate();
                        Thread.Sleep(1000);

                        // random choice of position.
                        Phase3KeyPressParadigm('?');
                        tb.Invalidate();
                        Thread.Sleep(1000);

                        // random slight advance.
                        Phase3KeyPressParadigm('>');
                        tb.Invalidate();
                        Thread.Sleep(1000);
                    }
                }
                */

                Phase3KeyPressParadigm(Char.ToLower(c));
            }
        }

        // The WEB has book numbers from 002 to 092.
        // The KJV has book numbers from 001 to 066.

        List<string> validBookNumberStrings = new List<string>();
        int indexOfCurrentBookNumStr = 0;

        // These control tf-idf randoms
        int highestTermCountLogScaled = 0;
        int bookCountScaled = 0;

        // This lookup uses that three-digit book number string.
        SortedList<string, string> bookNumStrToBookBody = new System.Collections.Generic.SortedList<string, string>();

        // First line in each book is the book number and title.
        SortedList<string, string> bookNumStrToTitle = new System.Collections.Generic.SortedList<string, string>();

        // Title lengths including CR LF for precise randoms.
        SortedList<string, int> bookNumStrToTitleLength = new SortedList<string, int>();

        // Post-title book body length, for precise randoms.
        SortedList<string, int> bookNumStrToRealBookBodyLength = new SortedList<string, int>();

        void PrepareCorpusIndexScreen()
        {
            // after the k or w keyin.

            // Add which bible was chosen to top of form
            this.Text = FormCaptionPrefix + ": " + corpusTitle;

            // embedded bible books are strictly formatted.
            string[] bibleBooks = corpusActive.Split('#', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (string bookBody in bibleBooks)
            {
                string bookNumber = bookBody.Substring(0, 3);
                bookNumStrToBookBody.Add(bookNumber, bookBody);
            }

            // prepare an index to look up books.
            validBookNumberStrings = bookNumStrToBookBody.Keys.ToList<string>();
            bookCountScaled = validBookNumberStrings.Count * 3 / 2;

            // Build a list of book numbers and titles
            // after Phase 2 screen introduction text.
            StringBuilder sb = new StringBuilder();

            // Start with quick user instructions.
            sb.Append(BookIndexScreenPrefix);

            // Showing the index for Which corpus?
            sb.Append(corpusTitle + " Index:\r\n\r\n");

            foreach (KeyValuePair<string, string> kvp in bookNumStrToBookBody)
            {
                // first line of each book body is the book number and title.
                int titleLineLength = kvp.Value.IndexOf('\r') + 2;
                sb.Append(kvp.Value.Substring(0, titleLineLength));

                // Save each title without CRLF for caption text
                string bookNumber = kvp.Key;
                bookNumStrToTitle.Add(bookNumber, kvp.Value.Substring(0, titleLineLength - 2));

                // And record their lengths for later randoms
                bookNumStrToTitleLength.Add(bookNumber, titleLineLength);
                bookNumStrToRealBookBodyLength.Add(bookNumber, kvp.Value.Length - titleLineLength);
            }
            Redraw_Textbox(sb.ToString(), 0);
        }

        // After two introduction screens above,
        // viewport text is derived from these:
        string bookBodyTextOfCurrentBook = "";
        int viewCharOffsetInEntireCurrentBook = 0; // includes title length

        // And for more precise randoms, and caption:
        int titleLineLengthInCurrentBook = 0;
        int bookBodyLengthInCurrentBook = 0;
        int viewCharOffsetInCurrentBookBody = 0;
        string titleWithoutNewlineInCurrentBook = "";


        // *************************************
        // But first, I have lots of ideas about
        // what constitutes pleasant randomness:
        // *************************************

        // For every purely alphabetic string,
        // make lookup dictionaries regarding:
        // 1. How many of the word are in the bible?
        // 2. How many bible books contain the word?
        // 3. Has word ever had initial lowercase?
        // (Thus that word is not a proper name.)
        //
        // 4. At end compute all logs of qtyTerms+1.
        // E.g.,
        // log(1) = 0
        // log(2) = 0.69314718055995
        // log(74000) = 11.211820372186
        //
        // 5. Also later, word length affects randoms.
        class termData
        {
            public int qtyTermInCorpus;
            public int qtydocsWithTerm;
            public bool everFirstLower;
            public int qtyTermInCorpusLogScaled;
        }

        // This will aggregate data over all books
        Dictionary<string, termData> lcTermToData = new Dictionary<string, termData>();

        static Regex RegexSplitAlphaTokens = new Regex("[^A-Za-z]+", RegexOptions.Compiled);

        private void PrepareCorpusMeasurements()
        {
            // do for all the bible books
            for (int i = 0; i < validBookNumberStrings.Count; i++)
            {
                // this three-digit string looks up all things:
                string curBookNumStr = validBookNumberStrings[i];
                string curBookBody = bookNumStrToBookBody[curBookNumStr];

                // Use two struct members to tabulate data for one book.
                Dictionary<string, termData> lcTermFacts = new Dictionary<string, termData>();

                // split the book into alphabetic tokens
                string[] tokens = RegexSplitAlphaTokens.Split(curBookBody);

                foreach (string token in tokens)
                {
                    if (token.Length == 0)
                        continue;
                    string lcTerm = token.ToLower();
                    if (lcTermFacts.ContainsKey(lcTerm) == false)
                    {
                        lcTermFacts.Add(lcTerm, new termData());
                    }
                    lcTermFacts[lcTerm].qtydocsWithTerm = 1;
                    lcTermFacts[lcTerm].qtyTermInCorpus++;
                    if (Char.IsLower(token[0]))
                    {
                        lcTermFacts[lcTerm].everFirstLower = true;
                    }
                }
                // finish this book by aggregating to overall:
                foreach (KeyValuePair<string, termData> kvp in lcTermFacts)
                {
                    if (lcTermToData.ContainsKey(kvp.Key) == false)
                        lcTermToData.Add(kvp.Key, new termData());
                    lcTermToData[kvp.Key].qtydocsWithTerm++;
                    lcTermToData[kvp.Key].qtyTermInCorpus += kvp.Value.qtyTermInCorpus;
                    lcTermToData[kvp.Key].everFirstLower |= kvp.Value.everFirstLower;
                }
            }

            // final datum:
            int highestTermCount = 0;
            int highestTermCountLog = 0;
            foreach (termData td in lcTermToData.Values)
            {
                // also now, fill in the log values
                // this first scaling is to use int, avoid doubles
                td.qtyTermInCorpusLogScaled = (int)Math.Log(td.qtyTermInCorpus + 1) * 10000;

                if (highestTermCount < td.qtyTermInCorpus)
                {
                    highestTermCount = td.qtyTermInCorpus;
                    highestTermCountLog = td.qtyTermInCorpusLogScaled;

                }
            }
            // this second scaling is to keep all words in play.
            highestTermCountLogScaled = highestTermCountLog * 3 / 2;

            /* Another diagnostic to guide coding...

            List<string> keys = lcTermToData.Keys.ToList<string>();
            SortedList<int, int> lenQty = new SortedList<int, int>();
            List<string> shorties = new List<string>();
            List<string> longies = new List<string>();
            foreach (string key in keys)
            {
                int len = key.Length;
                if (lenQty.ContainsKey(len) == false)
                {
                    lenQty.Add(len, 1);
                }
                else
                {
                    lenQty[len]++;
                }
                if (len < 2)
                    shorties.Add(key);
                if (len > 15)
                    longies.Add(key);
            }
            StringBuilder sb = new StringBuilder();
            foreach (KeyValuePair<int, int> kvp in lenQty)
            {
                sb.Append(kvp.Key.ToString("d3") + ": " + kvp.Value.ToString().PadLeft(7) + "\r\n");
            }
            foreach (string s in shorties)
            {
                sb.Append(s + "\r\n");
            }
            foreach (string s in longies)
            {
                sb.Append(s + "\r\n");
            }
            Redraw_Textbox(sb.ToString(), 0);

            ... In KJV or WEB, the sweet spot is about 6-7 chars:

            KJV:
            001:       4
            002:      39
            003:     307
            004:    1093
            005:    1720
            006:    2173
            007:    2203
            008:    1786
            009:    1382
            010:     923
            011:     503
            012:     240
            013:     107
            014:      42
            015:      22
            016:       8
            017:       1
            018:       1
            a
            i
            s
            o
            kibrothhattaavah
            bashanhavothjair
            evilfavouredness
            chepharhaammonai
            chushanrishathaim
            selahammahlekoth
            lovingkindnesses
            mahershalalhashbaz
            covenantbreakers
            unprofitableness

            WEB:
            001:       6
            002:      44
            003:     332
            004:    1218
            005:    2114
            006:    2592
            007:    2527
            008:    2176
            009:    1670
            010:    1062
            011:     576
            012:     261
            013:     104
            014:      35
            015:      13
            016:       3
            s
            a
            i
            t
            m
            o
            responsibilities
            incomprehensible
            incorruptibility
            ... */
        }

        static Random rand = new Random();

        static string accumulate3DigitStr = "";
        void Phase3KeyPressParadigm(char lcChar)
        {
            // (Also handles digits or / during phase 2.)

            // Firstly, keystrokes that may make book changes.

            bool setNewBook = false;
            if (lcChar == '/')
            {
                // choose a new random book
                indexOfCurrentBookNumStr = rand.Next() % validBookNumberStrings.Count;
                setNewBook = true;
            }
            if (char.IsDigit(lcChar))
            {
                accumulate3DigitStr += lcChar;
                if (accumulate3DigitStr.Length >= 3)
                {
                    int n = validBookNumberStrings.IndexOf(accumulate3DigitStr);
                    if (n > -1)
                    {
                        indexOfCurrentBookNumStr = n;
                        setNewBook = true;
                    }
                    accumulate3DigitStr = "";
                }
            }
            else
            {
                // Any and every non-digit resets accumulator.
                accumulate3DigitStr = "";
            }

            if (setNewBook)
            {
                // Leave phase 2, if there.
                inBookIndexScreen = false;

                string numStr = validBookNumberStrings[indexOfCurrentBookNumStr];

                // Set these globals from the pantry
                bookBodyTextOfCurrentBook = bookNumStrToBookBody[numStr];
                bookBodyLengthInCurrentBook = bookNumStrToRealBookBodyLength[numStr];
                titleWithoutNewlineInCurrentBook = bookNumStrToTitle[numStr];
                titleLineLengthInCurrentBook = bookNumStrToTitleLength[numStr];

                // I set these globals myself, as will setValidNewPosition later.
                viewCharOffsetInEntireCurrentBook = 0; // else titleLineLengthInCurrentBook;
                viewCharOffsetInCurrentBookBody = 0; // not quite true as would be negative

                // Also prepare the caption for initial top-of-book myself.
                this.Text = FormCaptionPrefix + ": at 0.000 of " +
                    corpusTrigraph + " " + titleWithoutNewlineInCurrentBook;

                RedrawBookSectionFirstTime();
            }
            else if (char.IsDigit(lcChar) == false)
            {
                // Secondly, for questions other than book changes.
                // which means, test if c shall unveil a character.

                // But first: let '<' regress, '> advance,
                // and '?' perform a random jump in book.

                if (lcChar == '<')
                {
                    int amount = typicalAdvance();
                    setValidNewPosition(viewCharOffsetInCurrentBookBody - amount);
                    RedrawBookSectionFirstTime();
                }

                if (lcChar == '>')
                {
                    int amount = typicalAdvance();
                    setValidNewPosition(viewCharOffsetInCurrentBookBody + amount);
                    RedrawBookSectionFirstTime();
                }

                if (lcChar == '?')
                {
                    setValidNewPosition(rand.Next());
                    RedrawBookSectionFirstTime();
                }

                // let space give away one character,
                // the same as typing the correct char.
                if (lcChar == ' ' ||
                    lcChar == char.ToLower(currentViewOriginalBookSection[currentViewNextUnderscoreIndex]))
                {
                    // Overwrite the _ smitten char with case sensitive original char
                    currentViewSmittenBookSection[currentViewNextUnderscoreIndex] = currentViewOriginalBookSection[currentViewNextUnderscoreIndex];
 
                    // find the next _ in view
                    bool found = false;
                    for (int i = currentViewNextUnderscoreIndex; i < currentViewSmittenBookSection.Length; i++)
                    {
                        if (currentViewSmittenBookSection[i] == '_')
                        {
                            found = true;
                            currentViewNextUnderscoreIndex = i;
                            break;
                        }
                    }
                    if (found)
                    {
                        string toshow = new string(currentViewSmittenBookSection);
                        Redraw_Textbox(toshow, currentViewNextUnderscoreIndex);
                    }
                    else
                    {
                        // There are no more underscores in current view.
                        // perform the next random typical/slight advance
                        int amount = typicalAdvance();
                        setValidNewPosition(viewCharOffsetInCurrentBookBody + amount);
                        RedrawBookSectionFirstTime();
                    }
                }
            }
        }

        int typicalAdvance()
        {
            // called for several keypresses
            // and one caller negates value.

            // two randoms centralize spread
            const int HALF = TYPICAL_ADVANCE / 2;
            return HALF + rand.Next() % HALF + rand.Next() % HALF;
        }

        void setValidNewPosition(int suggestedNewBodyPosition)
        {
            // This max endpoint ensures safety of hurting loop
            int maxStartPosition = bookBodyLengthInCurrentBook - DISTRESS_LENGTH;

            // If caller exceeded either boundary, modulus wraps around.
            int newStartPosition = suggestedNewBodyPosition % maxStartPosition;

            // fix negatives out of modulus operator
            if (newStartPosition < 0)
                newStartPosition += maxStartPosition;

            // I set globals myself
            viewCharOffsetInCurrentBookBody = newStartPosition;
            viewCharOffsetInEntireCurrentBook = newStartPosition + titleLineLengthInCurrentBook;

            // Update the form1 caption upon moving view.
            // New position is what fraction of random range?
            int fraction = 1000 * newStartPosition / maxStartPosition;

            // Show as 0.xxx
            this.Text = FormCaptionPrefix + ": at 0." +
                fraction.ToString().PadLeft(3, '0') + " of " +
                corpusTrigraph + " " + titleWithoutNewlineInCurrentBook;
        }

        // After redrawing and smiting chars, these globals
        // will communicate back to keypress event handler
        // view text and next underscore in current view.

        // Original means text before hurt by underscores:
        string currentViewOriginalBookSection = "";
        // Smitten means text hurt by underscores:
        char[] currentViewSmittenBookSection = [];
        // Critical to keep right:
        int currentViewNextUnderscoreIndex = -1;
        private void RedrawBookSectionFirstTime()
        {
            string visibleTextSelection = bookBodyTextOfCurrentBook.Substring(viewCharOffsetInEntireCurrentBook);

            // almost always
            if (visibleTextSelection.Length > TEXTBOX_CAPACITY)
                visibleTextSelection = visibleTextSelection.Substring(0, TEXTBOX_CAPACITY);

            // FIRST, word-align boundaries of textbox,
            // the same as I shall do soon for smiting,
            // but easier: puts whole words at borders.
            int discardAtop = 0;

            string optionalPrefix = "";

            // At top-of-book, make no leading adjustments
            if (viewCharOffsetInEntireCurrentBook > 0)
            {
                optionalPrefix = "... ";
                for (int i = 0; i < visibleTextSelection.Length - 1; i++)
                {
                    if (char.IsLetter(visibleTextSelection[i + 1])
                        && char.IsLetter(visibleTextSelection[i]) == false)
                    {
                        discardAtop = i + 1;
                        break;
                    }
                }
            }

            // Always make trailing adjustment,
            // even if by chance at end of book
            int discardPast = 0;
            for (int i = visibleTextSelection.Length - 2; i > 0; i--)
            {
                if (char.IsLetter(visibleTextSelection[i])
                    && char.IsLetter(visibleTextSelection[i + 1]) == false)
                {
                    discardPast = i + 1;
                    break;
                }
            }

            // now with complete words at both boundaries
            visibleTextSelection = visibleTextSelection.Substring(discardAtop, discardPast - discardAtop);

            // decorate the view boundaries
            visibleTextSelection = optionalPrefix + visibleTextSelection + " ...";

            // These globals govern the keypress event:
            // BTW, this text amount is the WHOLE view,
            // which usually has only middle/3 smitten.
            currentViewOriginalBookSection = visibleTextSelection;
            currentViewSmittenBookSection = visibleTextSelection.ToCharArray();
            currentViewNextUnderscoreIndex = -1;

            // This first time any novel book section is displayed
            // this loop will randomly strike out some whole words.
            // Later, key presses will replace chars and redisplay.

            // I MUST loop until I write at least one '_'.
            // Better, loop until some amount of them.

            // Visual ABOVE is numerically less than visual BELOW!
            int above = (currentViewSmittenBookSection.Length - DISTRESS_LENGTH) / 2;

            // seek a prior non-letter near top to range
            while (--above > 0)
            {
                if (char.IsLetter(currentViewSmittenBookSection[above]) == false)
                    break;
            }
            // if not bkwd, seek fwd non-letter
            if (above <= 0)
            {
                for (; ; )
                {
                    above++;
                    if (char.IsLetter(currentViewSmittenBookSection[above]) == false)
                        break;
                }
            }

            // repeat opposite for below, near end of range
            int below = above + DISTRESS_LENGTH;

            // seek a following non-letter near end to range
            while (++below < currentViewSmittenBookSection.Length - 1)
            {
                if (char.IsLetter(currentViewSmittenBookSection[below]) == false)
                    break;
            }
            // if not fwd, seek bkwd non-letter
            if (below >= currentViewSmittenBookSection.Length - 1)
            {
                for (; ; )
                {
                    below--;
                    if (char.IsLetter(currentViewSmittenBookSection[below]) == false)
                        break;
                }
            }

            // ABOVE is numerically less than BELOW!
            int between = below - above - 1;

            // Do not stop looping until reach target hurt level.

            int qtyCharsToHurt = between / 10; // season to taste.
            int qtyCharsZapped = 0;

            // this is outer loop
            while (qtyCharsZapped < qtyCharsToHurt)
            {
                // Inner loop (re-)scans interval to distress.
                char priorLoopChar = 'x';
                int indexAtopWord = -1;

                // counterintuitive, worth one coding error:
                // above is physically ABOVE, numerically less
                // below is physically BELOW, numerically more

                // this is inner loop
                for (int i = above + 1; i < below; i++)
                {
                    // work in the smitten view, as it is changing!
                    char c = currentViewSmittenBookSection[i];

                    // look for some next word start
                    if (char.IsLetter(c) && !char.IsLetter(priorLoopChar))
                    {
                        // at first char of new word
                        indexAtopWord = i;
                    }

                    // look for word ended,
                    // but only if started.
                    if (indexAtopWord != -1 &&
                        ! char.IsLetter(c) && char.IsLetter(priorLoopChar))
                    {
                        // i is past end of that word
                        int wordLen = i - indexAtopWord;
                        string word = new string(currentViewSmittenBookSection, indexAtopWord, wordLen);
                        string lcWord = word.ToLower();

                        // let us consider that word,
                        // whether or not to hurt it.
                        bool hurtword = true;

                        // now decrease that bool some.

                        // forgive short and long words
                        // sweet spot is at 6-7 letters.

                        // treat two asymmetrical tails
                        int len10 = wordLen * 10;
                        if (len10 < 70)
                        {
                            // 1-6 chars -> len10 10-60
                            if (rand.Next() % 80 > len10)
                                hurtword = false;
                        }
                        else
                        {
                            // 7-20 chars -> len10 70-200
                            if (40 + rand.Next() % 300 < len10)
                                hurtword = false;
                        }

                        /* Another coding diagnostic...
                         * 
                        // Make a histogram probability plot,
                        // but just regarding wordLen effect.
                        histoWordsSeen[wordLen]++;
                        */

                        // but don't hurt single char words,
                        // like possesive's and 't in don't

                        if (wordLen == 1)
                            hurtword = false;

                        // Now let us skip over frequent terms
                        // and terms that appear in many books.
                        // but not if they may be proper names.

                        termData td = lcTermToData[lcWord];
                        if(td.everFirstLower)
                        {
                            // docs looked good immediately
                            if (td.qtydocsWithTerm > rand.Next() % bookCountScaled)
                            {
                                // diagnostic... tallyHurtQtyDocs(lcWord);
                                hurtword = false;
                            }

                            // initially terms did not keep many,
                            // except top terms "the" and "and".
                            // Therefore changed to use log(qty).
                            if (td.qtyTermInCorpusLogScaled > rand.Next() % highestTermCountLogScaled)
                            {
                                // diagnostic... tallyHurtQtyTerm(lcWord);
                                hurtword = false;
                            }
                        }

                        if (hurtword)
                        {
                            // diagnostic... histoWordsHurt[wordLen]++;

                            for (int j = indexAtopWord; j < i; j++)
                            {
                                currentViewSmittenBookSection[j] = '_';
                            }
                            qtyCharsZapped += wordLen;
                        }
                        indexAtopWord = -1;
                    }
                    priorLoopChar = c;
                }
            }

            // The smiting loop is finished.
            // I guaranteed >= 1 underscore!
            string toshow = new string(currentViewSmittenBookSection);
            currentViewNextUnderscoreIndex = toshow.IndexOf('_');            
            Redraw_Textbox(toshow, currentViewNextUnderscoreIndex);
        }


        /* Another coding diagnostic, or three...
         * 
        // global per-app-run percentage histogram
        static int[] histoWordsSeen = new int[30];
        static int[] histoWordsHurt = new int[30];

        private void TallyWordLengthEffect()
        {
            StringBuilder sb = new StringBuilder();
            for(int i = 0; i < 30; i++)
            {
                int top = histoWordsHurt[i];
                int btm = histoWordsSeen[i];
                if(btm > 0)
                {
                    int pct = 100 * top / btm;
                    sb.Append(i.ToString().PadLeft(2) + ": " +
                        top.ToString().PadLeft(6) + " / " +
                        btm.ToString().PadLeft(6) + " = " +
                        pct.ToString().PadLeft(3) + "%\r\n");
                }
            }
            File.WriteAllText("""C:\a\t\TallyLength""" + corpusTrigraph + ".txt", sb.ToString());
        }

        Dictionary<string, int> hurtQtyDocs = new Dictionary<string, int>();

        void tallyHurtQtyDocs(string lcWord)
        {
            if(hurtQtyDocs.ContainsKey(lcWord) == false)
            {
                hurtQtyDocs.Add(lcWord, 1);
            }
            else
            {
                hurtQtyDocs[lcWord]++;
            }
        }

        Dictionary<string, int> hurtQtyTerm = new Dictionary<string, int>();

        void tallyHurtQtyTerm(string lcWord)
        {
            if (hurtQtyTerm.ContainsKey(lcWord) == false)
            {
                hurtQtyTerm.Add(lcWord, 1);
            }
            else
            {
                hurtQtyTerm[lcWord]++;
            }
        }
        private void TallyDocFreqEffect()
        {
            List<string> tosort = new List<string>();
            foreach(KeyValuePair<string, int> kvp in hurtQtyDocs)
            {
                tosort.Add(kvp.Value.ToString().PadLeft(5) + " " + kvp.Key);
            }
            tosort.Sort();
            tosort.Reverse();
            File.WriteAllLines("""C:\a\t\TallyDocs""" + corpusTrigraph + ".txt", tosort);
        }

        private void TallyTermFreqEffect()
        {
            List<string> tosort = new List<string>();
            foreach (KeyValuePair<string, int> kvp in hurtQtyTerm)
            {
                tosort.Add(kvp.Value.ToString().PadLeft(5) + " " + kvp.Key);
            }
            tosort.Sort();
            tosort.Reverse();
            File.WriteAllLines("""C:\a\t\TallyTerm""" + corpusTrigraph + ".txt", tosort);
        }
        *
        */
    }
}