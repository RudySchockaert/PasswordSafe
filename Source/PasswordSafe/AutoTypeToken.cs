﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace Medo.Security.Cryptography.PasswordSafe {
    /// <summary>
    /// Token representing either one key or command.
    /// </summary>
    [DebuggerDisplay("{Text}")]
    public class AutotypeToken {

        internal AutotypeToken(string content)
            : this(content, AutotypeTokenKind.Key) {
        }

        internal AutotypeToken(string content, AutotypeTokenKind type) {
            this.Content = content;
            this.Kind = type;
        }


        /// <summary>
        /// Gets text.
        /// </summary>
        public string Content { get; }

        /// <summary>
        /// Gets token type.
        /// </summary>
        public AutotypeTokenKind Kind { get; }


        /// <summary>
        /// Returns Text if token is Key, otherwise Command:Argument.
        /// </summary>
        public override string ToString() {
            return this.Content;
        }


        #region Auto-type

        /// <summary>
        /// Return auto-type tokens without any field processing; i.e. UserName won't be converted to actual user name.
        /// </summary>
        /// <param name="autotypeText">Auto-type text.</param>
        public static IEnumerable<AutotypeToken> GetAutotypeTokens(string autotypeText) {
            if (string.IsNullOrEmpty(autotypeText)) {
                yield return new AutotypeToken("UserName", AutotypeTokenKind.Command);
                yield return new AutotypeToken("{Tab}");
                yield return new AutotypeToken("Password", AutotypeTokenKind.Command);
                yield return new AutotypeToken("{Tab}");
                yield return new AutotypeToken("{Enter}");
            } else {
                var state = AutoTypeState.Default;
                string command = null;
                var sbCommandArguments = new StringBuilder();
                foreach (var ch in autotypeText) {
                    switch (state) {
                        case AutoTypeState.Default:
                            if (ch == '\\') {
                                state = AutoTypeState.Escape;
                            } else {
                                yield return new AutotypeToken(ch.ToString());
                            }
                            break;

                        case AutoTypeState.Escape:
                            switch (ch) {
                                case 'u':
                                case 'p':
                                case '2':
                                case 'g':
                                case 'i':
                                case 'l':
                                case 'm':
                                case 'z': //single character escape
                                    yield return GetCommandToken(ch.ToString(), null);
                                    state = AutoTypeState.Default;
                                    break;

                                case 'c': //double character escape
                                    state = AutoTypeState.EscapeCreditCard;
                                    break;

                                case 'b':
                                    yield return new AutotypeToken("{Backspace}");
                                    state = AutoTypeState.Default;
                                    break;

                                case 't':
                                    yield return new AutotypeToken("{Tab}");
                                    state = AutoTypeState.Default;
                                    break;

                                case 's':
                                    yield return new AutotypeToken("+{Tab}");
                                    state = AutoTypeState.Default;
                                    break;

                                case 'n':
                                    yield return new AutotypeToken("{Enter}");
                                    state = AutoTypeState.Default;
                                    break;

                                case 'd':
                                case 'w':
                                case 'W': //mandatory number characters
                                    command = ch.ToString();
                                    state = AutoTypeState.EscapeMandatoryNumber;
                                    break;

                                case 'o': //optional number characters
                                    command = ch.ToString();
                                    state = AutoTypeState.EscapeOptionalNumber;
                                    break;

                                default: //if escape doesn't exist
                                    yield return new AutotypeToken(ch.ToString());
                                    state = AutoTypeState.Default;
                                    break;
                            }
                            break;

                        case AutoTypeState.EscapeCreditCard:
                            switch (ch) {
                                case 'n':
                                case 'e':
                                case 'v':
                                case 'p': //double character escapes
                                    yield return GetCommandToken("c" + ch.ToString(), null);
                                    state = AutoTypeState.Default;
                                    break;

                                default: //if escape doesn't exist
                                    foreach (var key in AutotypeToken.GetIndividualKeyTokens("c" + ch)) {
                                        yield return key;
                                    }
                                    state = AutoTypeState.Default;
                                    break;
                            }
                            break;

                        case AutoTypeState.EscapeMandatoryNumber:
                            if (char.IsDigit(ch)) {
                                sbCommandArguments.Append(ch);
                                state = AutoTypeState.EscapeOptionalNumber;
                            } else {
                                foreach (var key in AutotypeToken.GetIndividualKeyTokens(command + sbCommandArguments.ToString() + ch)) {
                                    yield return key;
                                }
                                command = null;
                                state = AutoTypeState.Default;
                            }
                            break;

                        case AutoTypeState.EscapeOptionalNumber:
                            if (char.IsDigit(ch) && (sbCommandArguments.Length < 3)) {
                                sbCommandArguments.Append(ch);
                            } else {
                                yield return GetCommandToken(command, sbCommandArguments.ToString());
                                command = null; sbCommandArguments.Length = 0;
                                if (ch == '\\') {
                                    state = AutoTypeState.Escape;
                                } else {
                                    yield return new AutotypeToken(ch.ToString());
                                    state = AutoTypeState.Default;
                                }
                            }
                            break;

                        default: throw new NotImplementedException("Unknown state");
                    }
                }

                if (command != null) {
                    if ((sbCommandArguments.Length == 0) && (command.Equals("d", StringComparison.Ordinal) || command.Equals("w", StringComparison.Ordinal) || command.Equals("W", StringComparison.Ordinal))) {
                        foreach (var key in AutotypeToken.GetIndividualKeyTokens(command)) {
                            yield return key;
                        }
                    } else {
                        yield return GetCommandToken(command, sbCommandArguments.ToString());
                    }
                } else if (state == AutoTypeState.Escape) {
                    yield return new AutotypeToken(@"\");
                }
            }
        }

        /// <summary>
        /// Return auto-type tokens with textual fields filled in. Command fields, e.g. Wait, are not filled.
        /// Following commands are possible:
        /// * TwoFactorCode: 6-digit code for two-factor authentication.
        /// * Delay: Delay between characters in milliseconds.
        /// * Wait: Pause in milliseconds.
        /// * Legacy: Switches processing to legacy mode.
        /// </summary>
        /// <param name="autotypeText">Auto-type text.</param>
        /// <param name="entry">Entry to base fill-in on.</param>
        /// <exception cref="System.ArgumentNullException">Entry cannot be null.</exception>
        public static IEnumerable<AutotypeToken> GetAutotypeTokens(string autotypeText, Entry entry) {
            if (entry == null) { throw new ArgumentNullException("entry", "Entry cannot be null."); }

            foreach (var token in GetAutotypeTokens(autotypeText)) {
                if (token.Kind == AutotypeTokenKind.Command) {
                    var parts = token.Content.Split(':');
                    var command = parts[0];
                    var argument = (parts.Length > 1) ? parts[1] : null;
                    switch (command) {
                        case "UserName": foreach (var key in AutotypeToken.GetIndividualKeyTokens(entry.UserName)) { yield return key; } break;
                        case "Password": foreach (var key in AutotypeToken.GetIndividualKeyTokens(entry.Password)) { yield return key; } break;
                        case "TwoFactorCode": yield return token; break;

                        case "CreditCardNumber": foreach (var key in AutotypeToken.GetIndividualKeyTokens(entry.CreditCardNumber)) { yield return key; } break;
                        case "CreditCardExpiration": foreach (var key in AutotypeToken.GetIndividualKeyTokens(entry.CreditCardExpiration)) { yield return key; } break;
                        case "CreditCardVerificationValue": foreach (var key in AutotypeToken.GetIndividualKeyTokens(entry.CreditCardVerificationValue)) { yield return key; } break;
                        case "CreditCardPin": foreach (var key in AutotypeToken.GetIndividualKeyTokens(entry.CreditCardPin)) { yield return key; } break;

                        case "Group": foreach (var key in AutotypeToken.GetIndividualKeyTokens(entry.Group)) { yield return key; } break;
                        case "Title": foreach (var key in AutotypeToken.GetIndividualKeyTokens(entry.Title)) { yield return key; } break;
                        case "Url": foreach (var key in AutotypeToken.GetIndividualKeyTokens(entry.Url)) { yield return key; } break;
                        case "Email": foreach (var key in AutotypeToken.GetIndividualKeyTokens(entry.Email)) { yield return key; } break;

                        case "Notes":
                            var noteLines = entry.Notes.Split(new string[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
                            if (string.IsNullOrEmpty(argument)) {
                                foreach (var key in AutotypeToken.GetIndividualKeyTokens(string.Join("\n", noteLines))) {
                                    yield return key;
                                }
                            } else {
                                int lineNumber;
                                if (int.TryParse(argument, NumberStyles.Integer, CultureInfo.InvariantCulture, out lineNumber)) {
                                    if (lineNumber <= noteLines.Length) {
                                        var lineText = noteLines[lineNumber - 1];
                                        if (lineText.Length > 0) {
                                            foreach (var key in AutotypeToken.GetIndividualKeyTokens(lineText)) {
                                                yield return key;
                                            }
                                        }
                                    }
                                }
                            }
                            break;

                        case "Delay": yield return token; break;
                        case "Wait": yield return token; break;
                        case "Legacy": yield return token; break;
                    }
                } else {
                    yield return token;
                }
            }
        }


        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Justification = "Code is straightforward list of returns.")]
        private static AutotypeToken GetCommandToken(string command, string argument) {
            switch (command) {
                case "u": return new AutotypeToken("UserName", AutotypeTokenKind.Command);
                case "p": return new AutotypeToken("Password", AutotypeTokenKind.Command);
                case "2": return new AutotypeToken("TwoFactorCode", AutotypeTokenKind.Command);
                case "cn": return new AutotypeToken("CreditCardNumber", AutotypeTokenKind.Command);
                case "ce": return new AutotypeToken("CreditCardExpiration", AutotypeTokenKind.Command);
                case "cv": return new AutotypeToken("CreditCardVerificationValue", AutotypeTokenKind.Command);
                case "cp": return new AutotypeToken("CreditCardPin", AutotypeTokenKind.Command);
                case "g": return new AutotypeToken("Group", AutotypeTokenKind.Command);
                case "i": return new AutotypeToken("Title", AutotypeTokenKind.Command);
                case "l": return new AutotypeToken("Url", AutotypeTokenKind.Command);
                case "m": return new AutotypeToken("Email", AutotypeTokenKind.Command);
                case "o": return new AutotypeToken("Notes" + (string.IsNullOrEmpty(argument) ? "" : ":" + argument), AutotypeTokenKind.Command);
                case "d": return new AutotypeToken("Delay" + (string.IsNullOrEmpty(argument) ? "" : ":" + argument), AutotypeTokenKind.Command);
                case "w": return new AutotypeToken("Wait" + (string.IsNullOrEmpty(argument) ? "" : ":" + argument), AutotypeTokenKind.Command);
                case "W": return new AutotypeToken("Wait" + (string.IsNullOrEmpty(argument) ? "" : ":" + argument + "000"), AutotypeTokenKind.Command);
                case "z": return new AutotypeToken("Legacy", AutotypeTokenKind.Command);
                default: return new AutotypeToken(command);
            }
        }

        private enum AutoTypeState {
            Default,
            Escape,
            EscapeCreditCard,
            EscapeMandatoryNumber,
            EscapeOptionalNumber,
            EscapeNumber,
        }


        /// <summary>
        /// Returns individual tokens from the text.
        /// </summary>
        /// <param name="content">Text.</param>
        public static IEnumerable<AutotypeToken> GetIndividualKeyTokens(string content) {
            foreach (var ch in content) {
                switch (ch) {
                    case '+':
                    case '^':
                    case '%':
                    case '~':
                    case '(':
                    case ')':
                    case '{':
                    case '}':
                    case '[':
                    case ']': yield return new AutotypeToken("{" + ch + "}"); break;
                    case '\b': yield return new AutotypeToken("{Backspace}"); break;
                    case '\n':
                    case '\r': yield return new AutotypeToken("{Enter}"); break;
                    case '\t': yield return new AutotypeToken("{Tab}"); break;
                    default: yield return new AutotypeToken(ch.ToString()); break;
                }
            }
        }

        #endregion

    }
}
