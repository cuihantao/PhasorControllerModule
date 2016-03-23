﻿//******************************************************************************************************
//  Song.cs - Gbtc
//
//  Copyright © 2012, Grid Protection Alliance.  All Rights Reserved.
//
//  Licensed to the Grid Protection Alliance (GPA) under one or more contributor license agreements. See
//  the NOTICE file distributed with this work for additional information regarding copyright ownership.
//  The GPA licenses this file to you under the Eclipse Public License -v 1.0 (the "License"); you may
//  not use this file except in compliance with the License. You may obtain a copy of the License at:
//
//      http://www.opensource.org/licenses/eclipse-1.0.php
//
//  Unless agreed to in writing, the subject software distributed under the License is distributed on an
//  "AS-IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. Refer to the
//  License for the specific language governing permissions and limitations.
//
//  Code Modification History:
//  ----------------------------------------------------------------------------------------------------
//  07/29/2009 - J. Ritchie Carroll
//       Generated original version of source code.
//  09/14/2009 - Stephen C. Wills
//       Added new header and license agreement.
//  12/14/2012 - Starlynn Danyelle Gilliam
//       Modified Header.
//
//******************************************************************************************************

#region [ Contributor License Agreements ]

/**************************************************************************\
   Copyright © 2009 - J. Ritchie Carroll
   All rights reserved.
  
   Redistribution and use in source and binary forms, with or without
   modification, are permitted provided that the following conditions
   are met:
  
      * Redistributions of source code must retain the above copyright
        notice, this list of conditions and the following disclaimer.
       
      * Redistributions in binary form must reproduce the above
        copyright notice, this list of conditions and the following
        disclaimer in the documentation and/or other materials provided
        with the distribution.
  
   THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDER "AS IS" AND ANY
   EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
   IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR
   PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
   CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
   EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
   PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
   PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY
   OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
   (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
   OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
  
\**************************************************************************/

#endregion

using System;
using System.Collections.Generic;
using System.Linq;

namespace GSF.Media.Music
{
    /// <summary>
    /// Allows creation of a synthesized musical score storing the resultant song in an
    /// in-memory wave file for play back or saving music to disk.
    /// </summary>
    /// <example>
    /// This example generates a multi-instrument chord:
    /// <code>
    /// using System;
    /// using GSF.Media;
    /// using GSF.Media.Music;
    ///
    /// static class Program
    /// {
    ///     static void Main()
    ///     {
    ///         Song song = new Song { Damping = Damping.Linear };
    ///
    ///         Console.WriteLine("Generating multi-instrument chord...");
    ///
    ///         song.AddNotes
    ///         (
    ///             new Note { Frequency = Note.C4, Value = 4, Timbre = Timbre.EvenHarmonicSeries },
    ///             new Note { Frequency = Note.C4, Value = 4, Timbre = Timbre.Clarinet },
    ///             new Note { Frequency = Note.C4, Value = 4, Timbre = Timbre.Organ },
    ///             new Note { Frequency = Note.E4, Value = 4, Timbre = Timbre.EvenHarmonicSeries },
    ///             new Note { Frequency = Note.E4, Value = 4, Timbre = Timbre.Clarinet },
    ///             new Note { Frequency = Note.E4, Value = 4, Timbre = Timbre.Organ },
    ///             new Note { Frequency = Note.G4, Value = 4, Timbre = Timbre.EvenHarmonicSeries },
    ///             new Note { Frequency = Note.G4, Value = 4, Timbre = Timbre.SimulatedClarinet },
    ///             new Note { Frequency = Note.G4, Value = 4, Timbre = Timbre.SimulatedOrgan }
    ///         );
    ///
    ///         song.Finish();
    ///
    ///         Console.WriteLine("Saving chord to disk...");
    ///         song.Save("MajorTriad.wav");
    ///
    ///         Console.WriteLine("Playing chord...");
    ///         song.Play();
    ///
    ///         Console.ReadKey();
    ///     }
    /// }
    /// </code>
    /// This example generates a familiar tune, plays the song and saves it to disk:
    /// <code>
    /// // Add reference to System.Speech
    /// using System;
    /// using System.IO;
    /// using GSF.Media;
    /// using GSF.Media.Music;
    /// using System.Speech;
    /// using System.Speech.Synthesis;
    /// using System.Speech.AudioFormat;
    /// 
    /// static class Program
    /// {
    ///     static void Main()
    ///     {
    ///         Console.WriteLine("Synthesizing speech...");
    ///         WaveFile speech = CreateSynthesizedVoiceOver();
    /// 
    ///         Console.WriteLine("Synthesizing song at tempo for use with speech...");
    /// 
    ///         // Define all the notes of jingle bells as a single phrase of music
    ///         Phrase score = CreateJingleBellsScore();
    /// 
    ///         // Create one song at a slower tempo to help with speech synchronization
    ///         Song speechTempSong = new Song { Tempo = new Tempo(160, NoteValue.Quarter) };
    /// 
    ///         // Make sure audio specifications for song and speech match
    ///         speechTempSong.SampleRate = speech.SampleRate;
    ///         speechTempSong.BitsPerSample = speech.BitsPerSample;
    ///         speechTempSong.Channels = speech.Channels;
    /// 
    ///         // Add all the notes to the song
    ///         speechTempSong.AddPhrase(score);
    ///         speechTempSong.Finish();
    /// 
    ///         Console.WriteLine("Synthesizing song by itself at normal tempo...");
    /// 
    ///         // Create one song at a slower tempo to help with speech synchronization
    ///         Song normalTempoSong = new Song();
    /// 
    ///         // Add all the notes to the song
    ///         normalTempoSong.AddPhrase(score);
    ///         normalTempoSong.Finish();
    /// 
    ///         Console.WriteLine("Saving normal tempo song to disk as \"JingleBells.wav\"...");
    ///         normalTempoSong.Save("JingleBells.wav");
    /// 
    ///         Console.WriteLine("Combining speech with song...");
    ///         WaveFile combined = WaveFile.Combine(speech, speechTempSong);
    /// 
    ///         Console.WriteLine("Saving combined work to disk as \"SingingComputer.wav\"...");
    ///         combined.Save("SingingComputer.wav");
    /// 
    ///         Console.WriteLine("Playing combined work...");
    ///         combined.Play();
    /// 
    ///         Console.ReadKey();
    ///     }
    /// 
    ///     private static WaveFile CreateSynthesizedVoiceOver()
    ///     {
    ///         SpeechSynthesizer synthesizer = new SpeechSynthesizer();
    ///         MemoryStream speechStream = new MemoryStream();
    ///         PromptBuilder songText = new PromptBuilder();
    /// 
    ///         synthesizer.SelectVoice("Microsoft Sam");
    ///         synthesizer.Rate = 5; // Range = -10 to +10
    ///         synthesizer.SetOutputToWaveStream(speechStream);
    /// 
    ///         songText.AppendText("Jin - gull bells!", PromptEmphasis.Strong);
    ///         songText.AppendBreak(new TimeSpan(3000000));
    ///         songText.AppendText("Jin - gull bells!", PromptEmphasis.Strong);
    ///         songText.AppendBreak(new TimeSpan(3000000));
    ///         songText.AppendText("Jin - gull - all, theuh - way!", PromptEmphasis.Strong);
    ///         songText.AppendBreak(new TimeSpan(12000000));
    ///         songText.AppendText("Oh - what - fun!", PromptEmphasis.Strong);
    ///         songText.AppendBreak(new TimeSpan(1000000));
    ///         songText.AppendText("It - is!", PromptEmphasis.Strong);
    ///         songText.AppendBreak(new TimeSpan(500000));
    ///         songText.AppendText("To - ride!", PromptEmphasis.Strong);
    ///         songText.AppendBreak(new TimeSpan(500000));
    ///         songText.AppendText("a - one - horse!", PromptEmphasis.Strong);
    ///         songText.AppendBreak(new TimeSpan(500000));
    ///         songText.AppendText("Open!", PromptEmphasis.Strong);
    ///         songText.AppendBreak(new TimeSpan(500000));
    ///         songText.AppendText("Sleigh!", PromptEmphasis.Strong);
    ///         songText.AppendBreak(new TimeSpan(1000000));
    ///         songText.AppendText("A!", PromptEmphasis.Strong);
    ///         songText.AppendBreak(new TimeSpan(2500000));
    ///         songText.AppendText("Jin - gull bells!", PromptEmphasis.Strong);
    ///         songText.AppendBreak(new TimeSpan(3000000));
    ///         songText.AppendText("Jin - gull bells!", PromptEmphasis.Strong);
    ///         songText.AppendBreak(new TimeSpan(3000000));
    ///         songText.AppendText("Jin - gull - all, theuh - way!", PromptEmphasis.Strong);
    ///         songText.AppendBreak(new TimeSpan(12000000));
    ///         songText.AppendText("Oh - what - fun!", PromptEmphasis.Strong);
    ///         songText.AppendBreak(new TimeSpan(1000000));
    ///         songText.AppendText("It - is!", PromptEmphasis.Strong);
    ///         songText.AppendBreak(new TimeSpan(500000));
    ///         songText.AppendText("To - ride!", PromptEmphasis.Strong);
    ///         songText.AppendBreak(new TimeSpan(500000));
    ///         songText.AppendText("a - one - horse!", PromptEmphasis.Strong);
    ///         songText.AppendBreak(new TimeSpan(500000));
    ///         songText.AppendText("Open!", PromptEmphasis.Strong);
    ///         songText.AppendBreak(new TimeSpan(500000));
    ///         songText.AppendText("Sleigh.", PromptEmphasis.Reduced);
    /// 
    ///         synthesizer.Speak(songText);
    ///         speechStream.Position = 0;
    ///         return WaveFile.Load(speechStream);
    ///     }
    /// 
    ///     private static Phrase CreateJingleBellsScore()
    ///     {
    ///         Phrase score = new Phrase();
    ///         Phrase passage = new Phrase();
    /// 
    ///         // Define the repeating phrase of the song
    ///         passage.AddNotes
    ///         (
    ///             new Note { Frequency = Note.B3, NamedValue = NoteValue.Quarter },
    ///             new Note { Frequency = Note.C3, NamedValue = NoteValue.Whole }
    ///         );
    ///         passage.AddNotes(new Note { Frequency = Note.B3, NamedValue = NoteValue.Quarter });
    ///         passage.AddNotes(new Note { Frequency = Note.B3, NamedValue = NoteValue.Half });
    /// 
    ///         passage.AddNotes
    ///         (
    ///             new Note { Frequency = Note.B3, NamedValue = NoteValue.Quarter },
    ///             new Note { Frequency = Note.G3, NamedValue = NoteValue.Whole }
    ///         );
    ///         passage.AddNotes(new Note { Frequency = Note.B3, NamedValue = NoteValue.Quarter });
    ///         passage.AddNotes(new Note { Frequency = Note.B3, NamedValue = NoteValue.Half });
    /// 
    ///         passage.AddNotes
    ///         (
    ///             new Note { Frequency = Note.B3, NamedValue = NoteValue.Quarter },
    ///             new Note { Frequency = Note.C3, NamedValue = NoteValue.Whole }
    ///         );
    ///         passage.AddNotes(new Note { Frequency = Note.D4, NamedValue = NoteValue.Quarter });
    ///         passage.AddNotes(new Note { Frequency = Note.G3, NamedValue = NoteValue.Quarter });
    ///         passage.AddNotes(new Note { Frequency = Note.A3, NamedValue = NoteValue.Quarter });
    /// 
    ///         passage.AddNotes
    ///         (
    ///             new Note { Frequency = Note.B3, NamedValue = NoteValue.Whole },
    ///             new Note { Frequency = Note.G3, NamedValue = NoteValue.Whole }
    ///         );
    /// 
    ///         passage.AddNotes
    ///         (
    ///             new Note { Frequency = Note.C4, NamedValue = NoteValue.Quarter },
    ///             new Note { Frequency = Note.D3, NamedValue = NoteValue.Whole }
    ///         );
    ///         passage.AddNotes(new Note { Frequency = Note.C4, NamedValue = NoteValue.Quarter });
    ///         passage.AddNotes(new Note { Frequency = Note.C4, NamedValue = NoteValue.Quarter });
    ///         passage.AddNotes(new Note { Frequency = Note.C4, NamedValue = NoteValue.Quarter });
    /// 
    ///         passage.AddNotes
    ///         (
    ///             new Note { Frequency = Note.C4, NamedValue = NoteValue.Quarter },
    ///             new Note { Frequency = Note.G3, NamedValue = NoteValue.Whole }
    ///         );
    ///         passage.AddNotes(new Note { Frequency = Note.B3, NamedValue = NoteValue.Quarter });
    ///         passage.AddNotes(new Note { Frequency = Note.B3, NamedValue = NoteValue.Quarter });
    ///         passage.AddNotes(new Note { Frequency = Note.B3, NamedValue = NoteValue.Quarter });
    /// 
    ///         score.AddPhrase(passage);
    /// 
    ///         score.AddNotes
    ///         (
    ///             new Note { Frequency = Note.B3, NamedValue = NoteValue.Quarter },
    ///             new Note { Frequency = Note.F3S, NamedValue = NoteValue.Whole }
    ///         );
    ///         score.AddNotes(new Note { Frequency = Note.A3, NamedValue = NoteValue.Quarter });
    ///         score.AddNotes(new Note { Frequency = Note.A3, NamedValue = NoteValue.Quarter });
    ///         score.AddNotes(new Note { Frequency = Note.B3, NamedValue = NoteValue.Quarter });
    /// 
    ///         score.AddNotes
    ///         (
    ///             new Note { Frequency = Note.A3, NamedValue = NoteValue.Half },
    ///             new Note { Frequency = Note.G3, NamedValue = NoteValue.Whole }
    ///         );
    ///         score.AddNotes(new Note { Frequency = Note.D4, NamedValue = NoteValue.Half });
    /// 
    ///         score.AddPhrase(passage);
    /// 
    ///         score.AddNotes
    ///         (
    ///             new Note { Frequency = Note.D4, NamedValue = NoteValue.Quarter },
    ///             new Note { Frequency = Note.G3, NamedValue = NoteValue.Whole },
    ///             new Note { Frequency = Note.F3, NamedValue = NoteValue.Whole }
    ///         );
    ///         score.AddNotes(new Note { Frequency = Note.D4, NamedValue = NoteValue.Quarter });
    ///         score.AddNotes(new Note { Frequency = Note.C4, NamedValue = NoteValue.Quarter });
    ///         score.AddNotes(new Note { Frequency = Note.A3, NamedValue = NoteValue.Quarter });
    /// 
    ///         score.AddNotes
    ///         (
    ///             new Note { Frequency = Note.G3, NamedValue = NoteValue.Whole },
    ///             new Note { Frequency = Note.E3, NamedValue = NoteValue.Whole }
    ///         );
    /// 
    ///         return score;
    ///     }
    /// }
    /// </code>
    /// </example>
    public class Song : WaveFile
    {
        #region [ Members ]

        // Fields
        private MeasureSize m_measureSize;
        private TimbreFunction m_timbre;
        private DampingFunction m_damping;
        private Tempo m_tempo;
        private readonly List<Note> m_noteQueue;
        private double m_dynamic;
        private double m_interNoteDelay;
        private long m_currentSample;

        #endregion

        #region [ Constructors ]

        /// <summary>
        /// Creates a new song with a 3/4 measure size, a tempo of 240 quarter-notes per minute,
        /// mezzo-forte prevailing dynamic level, using a basic note timbre and standard CD-quality
        /// settings for the underlying sound file.
        /// </summary>
        public Song()
        {
            m_measureSize = new MeasureSize(3, NoteValue.Quarter);
            m_tempo = new Tempo(240, NoteValue.Quarter);
            m_dynamic = (double)Music.Dynamic.MezzoForte / 100.0D;
            m_timbre = Music.Timbre.BasicNote;
            m_damping = Music.Damping.Natural;
            m_noteQueue = new List<Note>();
        }

        /// <summary>
        /// Creates a new song with a 3/4 measure size, a tempo of 240 quarter-notes per minute,
        /// mezzo-forte prevailing dynamic level, using a basic note timbre and the specified
        /// audio format settings.
        /// </summary>
        /// <param name="sampleRate">Desired sample rate</param>
        /// <param name="bitsPerSample">Desired bits-per-sample</param>
        /// <param name="channels">Desired data channels</param>
        public Song(SampleRate sampleRate, BitsPerSample bitsPerSample, DataChannels channels)
            : base(sampleRate, bitsPerSample, channels)
        {
            m_measureSize = new MeasureSize(3, NoteValue.Quarter);
            m_tempo = new Tempo(240, NoteValue.Quarter);
            m_dynamic = (double)Music.Dynamic.MezzoForte / 100.0D;
            m_timbre = Music.Timbre.BasicNote;
            m_damping = Music.Damping.Natural;
            m_noteQueue = new List<Note>();
        }

        #endregion

        #region [ Properties ]

        /// <summary>
        /// Gets current measure size or defines a new measure size for the song.
        /// </summary>
        public MeasureSize MeasureSize
        {
            get
            {
                return m_measureSize;
            }
            set
            {
                m_measureSize = value;
            }
        }

        /// <summary>
        /// Gets current tempo or defines a new tempo for the song.
        /// </summary>
        public Tempo Tempo
        {
            get
            {
                return m_tempo;
            }
            set
            {
                m_tempo = value;
            }
        }

        /// <summary>
        /// Returns the time for a single beat.
        /// </summary>
        public double BeatTime
        {
            get
            {
                return m_tempo.CalculateNoteValueTime(m_measureSize.NamedNoteValue, 0);
            }
        }

        /// <summary>
        /// Gets or sets the default tibre function used to synthesize the sounds
        /// of the added notes (i.e., the instrument). This timbre function will be
        /// used if no other function is specified when adding notes.
        /// </summary>
        public TimbreFunction Timbre
        {
            get
            {
                return m_timbre;
            }
            set
            {
                m_timbre = value;
            }
        }

        /// <summary>
        /// Gets or sets the default damping function used to lower the sound volume
        /// of the added notes over time. This damping function will be used if no
        /// other function is specified when adding notes.
        /// </summary>
        public DampingFunction Damping
        {
            get
            {
                return m_damping;
            }
            set
            {
                m_damping = value;
            }
        }

        /// <summary>
        /// Gets or sets the prevailing named dynamic (i.e., volume) for the song.  Individual notes
        /// can choose to override this dynamic.
        /// </summary>
        public Dynamic NamedDynamic
        {
            get
            {
                // Dynamic can be custom, so return closest match...
                int dynamic = (int)m_dynamic * 100;

                if (dynamic <= (int)Music.Dynamic.Pianissimo)
                {
                    return Music.Dynamic.Pianissimo;
                }
                else if (dynamic <= (int)Music.Dynamic.Piano)
                {
                    return Music.Dynamic.Piano;
                }
                else if (dynamic <= (int)Music.Dynamic.MezzoPiano)
                {
                    return Music.Dynamic.MezzoPiano;
                }
                else if (dynamic <= (int)Music.Dynamic.MezzoForte)
                {
                    return Music.Dynamic.MezzoForte;
                }
                else if (dynamic <= (int)Music.Dynamic.Forte)
                {
                    return Music.Dynamic.Forte;
                }
                else
                {
                    return Music.Dynamic.Fortissimo;
                }
            }
            set
            {
                if (value == Music.Dynamic.Undefined)
                    value = Music.Dynamic.MezzoForte;

                m_dynamic = (double)value / 100.0D;
            }
        }

        /// <summary>
        /// Gets or sets the prevailing dynamic (i.e., volume) expressed as percentage in the range
        /// of 0 to 1 for the song. Individual notes can choose to override this dynamic.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Value must be expressed as a fractional percentage between zero and one.
        /// </exception>
        public double Dynamic
        {
            get
            {
                return m_dynamic;
            }
            set
            {
                if (value != -1.0D && (value < 0.0D || value > 1.0D))
                    throw new ArgumentOutOfRangeException("value", "Value must be expressed as a fractional percentage between zero and one");

                if (value == -1.0D)
                    m_dynamic = (double)Music.Dynamic.MezzoForte / 100.0D;
                else
                    m_dynamic = value;
            }
        }

        /// <summary>Injects specified rest time, in seconds, between notes.</summary>
        public double InterNoteDelay
        {
            get
            {
                return m_interNoteDelay;
            }
            set
            {
                m_interNoteDelay = value;
            }
        }

        #endregion

        #region [ Methods ]

        /// <summary>
        /// Add a predefined phrase of notes to the song.
        /// </summary>
        /// <param name="phrase">Phrase to add.</param>
        public void AddPhrase(Phrase phrase)
        {
            foreach (Note[] notes in phrase.Notes)
            {
                AddNotes(notes);
            }
        }

        /// <summary>
        /// Add a series of notes to the song.
        /// </summary>
        /// <param name="notes">Notes to add.</param>
        public void AddNotes(params Note[] notes)
        {
            double samplesPerSecond = SampleRate;
            long samplePeriod;

            // Calculate value time, start and end time indices for each note to be added
            foreach (Note note in notes)
            {
                // Calculate note value time given current tempo
                note.CalculateValueTime(m_tempo);

                // Assign relative note durations in terms of sample rate
                note.SamplePeriod = (long)(note.ValueTime * samplesPerSecond);
                note.StartTimeIndex = m_currentSample;
                note.EndTimeIndex = note.StartTimeIndex + note.SamplePeriod - 1;
            }

            // Assign sample period to note with shortest duration - all other notes
            // will remain in queue until they have completed their run
            samplePeriod = notes.Min(note => note.SamplePeriod);

            // Add notes to note queue
            m_noteQueue.AddRange(notes);

            // Add queued notes to song for given sample period
            AddQueuedNotesToSong(samplePeriod);
        }

        /// <summary>
        /// Called when there are no more notes to add.
        /// </summary>
        /// <remarks>
        /// This flushes the remaining queued notes into the song allowing them to run their remaining time.
        /// </remarks>
        public void Finish()
        {
            if (m_noteQueue.Count > 0)
            {
                double samplesPerSecond = SampleRate;
                long samplePeriod;

                // Assign sample period to note with longest duration - this makes sure all remaining
                // queued notes will complete their run
                samplePeriod = (long)(m_noteQueue.Max(note => note.ValueTime) * samplesPerSecond);

                // Add queued notes to song for given sample period
                AddQueuedNotesToSong(samplePeriod);
            }
        }

        /// <summary>
        /// Add a rest for the given length for the current beat.
        /// </summary>
        /// <param name="restLength">Duration of wait specified as a note value.</param>
        public void AddRest(double restLength)
        {
            AddNotes(new Note
            {
                Value = restLength
            });
        }

        /// <summary>
        /// Add a rest for the given length for the current beat.
        /// </summary>
        /// <param name="restLength">Duration of wait specified as a note value.</param>
        public void AddRest(NoteValue restLength)
        {
            AddNotes(new Note
            {
                NamedValue = restLength
            });
        }

        /// <summary>
        /// Add a rest for the given length for the current beat.
        /// </summary>
        /// <param name="restLength">Duration of wait specified as a note value.</param>
        public void AddRest(NoteValueBritish restLength)
        {
            AddNotes(new Note
            {
                NamedValueBritish = restLength
            });
        }

        /// <summary>
        /// Add a rest for the given length for the current beat.
        /// </summary>
        /// <param name="restLength">Duration of wait specified as a note value.</param>
        /// <param name="dots">Total dotted note length extensions to apply.</param>
        public void AddRest(NoteValue restLength, int dots)
        {
            AddNotes(new Note
            {
                NamedValue = restLength, Dots = dots
            });
        }

        /// <summary>
        /// Add a rest for the given length for the current beat.
        /// </summary>
        /// <param name="restLength">Duration of wait specified as a note value.</param>
        /// <param name="dots">Total dotted note length extensions to apply.</param>
        public void AddRest(NoteValueBritish restLength, int dots)
        {
            AddNotes(new Note
            {
                NamedValueBritish = restLength, Dots = dots
            });
        }

        /// <summary>
        /// Starts crescendo dynamic over the range of the specified number of beats.
        /// </summary>
        /// <param name="totalBeats">Total number of beats overwhich to gradually increase volume.</param>
        /// <param name="endDynamic">Desired volume when crescendo is complete.</param>
        /// <remarks>
        /// Current <see cref="Dynamic"/> is the starting dynamic which should be less than <paramref name="endDynamic"/>.
        /// </remarks>
        public void SetCrescendoDynamic(int totalBeats, Dynamic endDynamic)
        {
            // TODO: implement crescendo dynamic
        }

        /// <summary>
        /// Starts diminuendo dynamic over the range of the specified number of beats.
        /// </summary>
        /// <param name="totalBeats">Total number of beats overwhich to gradually decrease volume.</param>
        /// <param name="endDynamic">Desired volume when diminuendo is complete.</param>
        /// <remarks>
        /// Current <see cref="Dynamic"/> is the starting dynamic which should be greater than <paramref name="endDynamic"/>.
        /// </remarks>
        public void SetDiminuendoDynamic(int totalBeats, Dynamic endDynamic)
        {
            // TODO: implement diminuendo dynamic
        }

        private void AddQueuedNotesToSong(long samplePeriod)
        {
            List<int> completedNotes = new List<int>();
            TimbreFunction timbre;
            DampingFunction damping;
            double sample, dynamic;
            Note note;
            int x;

            for (long sampleIndex = m_currentSample; sampleIndex < m_currentSample + samplePeriod + (long)(m_interNoteDelay * SampleRate); sampleIndex++)
            {
                // Create summation of all notes at given time
                sample = 0.0D;

                for (x = 0; x < m_noteQueue.Count; x++)
                {
                    if (!completedNotes.Contains(x))
                    {
                        note = m_noteQueue[x];

                        if (sampleIndex < note.EndTimeIndex)
                        {
                            // Get timbre function
                            timbre = ((object)note.Timbre == null ? m_timbre : timbre = note.Timbre);

                            // Get damping function
                            damping = ((object)note.Damping == null ? m_damping : note.Damping);

                            // Get note dynamic
                            dynamic = (note.Dynamic == -1.0D ? m_dynamic : note.Dynamic);

                            // Generate note at given time
                            sample +=
                                timbre(note.Frequency, sampleIndex, note.SamplePeriod, SampleRate) *
                                damping(sampleIndex - m_currentSample, note.SamplePeriod, SampleRate) *
                                dynamic;
                        }
                        else
                        {
                            completedNotes.Add(x);
                        }
                    }
                }

                // Adjust sample amplitude so as to not adversely affect the volume of the song
                AddSample(sample % 1.0D * AmplitudeScalar);
            }

            m_currentSample += samplePeriod;

            // Remove completed notes from queue - removal is in reverse sorted
            // order to preserve indices
            completedNotes.Sort();
            completedNotes.Reverse();

            for (x = 0; x < completedNotes.Count; x++)
            {
                m_noteQueue.RemoveAt(completedNotes[x]);
            }
        }

        #endregion
    }
}
