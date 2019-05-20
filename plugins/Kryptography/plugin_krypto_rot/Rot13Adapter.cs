﻿using Kontract.Attributes;
using Kontract.Interfaces.Intermediate;
using Kryptography;
using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading.Tasks;
using Kontract.Interfaces;
using Kontract.Models;
using Kontract.Models.Intermediate;

namespace plugin_krypto_rot
{
    [Export(typeof(IPlugin))]
    [MenuStripExtension("General", "Rot13")]
    public class Rot13Adapter : ICipherAdapter
    {
        // ReSharper disable once MissingXmlDoc
        public event EventHandler<RequestDataEventArgs> RequestData;

        public string Name => "Rot13";

        public Task<bool> Decrypt(Stream toDecrypt, Stream decryptInto, IProgress<ProgressReport> progress)
        {
            return DoCipher(toDecrypt, decryptInto, progress, true);
        }

        public Task<bool> Encrypt(Stream toEncrypt, Stream encryptInto, IProgress<ProgressReport> progress)
        {
            return DoCipher(toEncrypt, encryptInto, progress, false);
        }

        private Task<bool> DoCipher(Stream input, Stream output, IProgress<ProgressReport> progress, bool decrypt)
        {
            return Task.Factory.StartNew(() =>
            {
                progress.Report(new ProgressReport { Percentage = 0, Message = decrypt ? "Decryption..." : "Encryption..." });

                using (var rot = new RotStream(decrypt ? input : output, 13))
                {
                    var buffer = new byte[0x10000];
                    var totalLength = decrypt ? rot.Length : input.Length;
                    while (rot.Position < totalLength)
                    {
                        var length = (int)Math.Min(0x10000, totalLength - rot.Position);

                        if (decrypt)
                        {
                            rot.Read(buffer, 0, length);
                            output.Write(buffer, 0, length);
                        }
                        else
                        {
                            input.Read(buffer, 0, length);
                            rot.Write(buffer, 0, length);
                        }

                        progress.Report(new ProgressReport { Percentage = (double)rot.Position / totalLength * 100, Message = decrypt ? "Decryption..." : "Encryption...", });
                    }
                }

                progress.Report(new ProgressReport { Percentage = 100, Message = decrypt ? "Decryption finished." : "Encryption finished." });

                return true;
            });
        }
    }
}
