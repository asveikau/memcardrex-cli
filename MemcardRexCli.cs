// Copyright (C) 2023 Andrew Sveikauskas
//
// GNU GPL v3; see LICENSE file for details.
//
// This is just a simple command line frontend for @ShendoXT's ps1card.cs from
// the memcardrex project.
//

using System;
using System.Linq;
using System.Text;

using MemcardRex;

public static class MemcardRexCli
{
   private const int DeletedMask = 0x4;

   private const int MaxSaves = 15;

   private enum SaveType
   {
      ActionReplay = 1,
      Mcs          = 2,
      Raw          = 3,
      Ps3          = 4,
   }

   private enum MemCardType
   {
      Raw         = 1,
      Gme         = 2,
      Vgs         = 3,
      Vmp         = 4,
      Mcx         = 5,
   }

   private static bool SaveTypeIsDeleted(byte type)
   {
      return type == 0 || (type & DeletedMask) != 0;
   }

   private static void Usage()
   {
      Console.Error.WriteLine(
          "usage:  {0} new card-filename [format]\n" +
          "        {0} list card-filename\n" +
          "        {0} export card-filename <index|name> [save-filename] " +
                                                        "[format]\n" +
          "        {0} import card-filename save-filename dest-index\n" +
          "        {0} delete card-filename <index|name>\n" +
          "        {0} erase card-filename <index|name>\n" +
          "        {0} convert src-card-filename dest-card-filename format\n" +
                  "\n" +
                  "Save formats: {1} -- default is raw\n" +
                  "Card formats: {2}\n"
                  ,
          "memcardrex-cli",
          String.Join(" ", Enum.GetNames(typeof(SaveType))),
          String.Join(" ", Enum.GetNames(typeof(MemCardType)))
      );
      Environment.Exit(1);
   }

   private static bool EnumIgnoreCase<T>(string s, out T o) where T:struct
   {
       o = default(T);
       var matchedCase = Enum.GetNames(typeof(T))
                             .Where(name =>
                                name.Equals(
                                   s,
                                   StringComparison.InvariantCultureIgnoreCase
                                )
                             )
                             .SingleOrDefault();
       return matchedCase != null && Enum.TryParse(matchedCase, out o);
   }

   private static T ParseEnumFromUser<T>(string e) where T:struct
   {
      T r = default(T);
      if (!EnumIgnoreCase<T>(e, out r))
      {
         Console.Error.WriteLine(
            "Unknown format: {0}\n" +
            "Supported formats: {1}",
            e,
            String.Join(" ", Enum.GetNames(typeof(T)))
         );
         Environment.Exit(1);
      }
      return r;
   }

   private static void MainMain(string [] args)
   {
      if (args.Length < 2)
         Usage();

      var cmd = args[0];
      var cardFile = args[1];

      var card = new ps1card();

      Action openCard = () =>
      {
         var res = card.openMemoryCard(cardFile, false);
         if (res != null)
         {
            Console.Error.WriteLine("Failed to open card {0}: {1}", cardFile ?? "<new>", res);
            Environment.Exit(1);
         }
      };
      Func<int, string> getFilename = (int i) =>
         String.Format(
            "{0}{1}{2}{3}",
            (char)(card.saveRegion[i] & 0xff),
            (char)(card.saveRegion[i] >> 8),
            card.saveProdCode[i],
            card.saveIdentifier[i]
         ).Replace("\0","");
      Func<string, int> findSave = (string s) =>
      {
         int i;

         if (int.TryParse(s, out i))
         {
            if (i >= 0 && i < MaxSaves)
               return i;
         }
         else
         {
            for (i=0; i<MaxSaves; ++i)
            {
               var name = getFilename(i);
               if (name.Equals(s, StringComparison.InvariantCultureIgnoreCase))
                  return i;
            }
         }

         return -1;
      };
      Action tooManyArgs = () =>
      {
          Console.Error.WriteLine("{0}: too many arguments", cmd);
          Usage();
      };
      Action save = () =>
      {
         if (card.changedFlag)
         {
            string file = card.cardLocation;
            int type = (int)card.cardType;
            bool repair = false;

            if (!card.saveMemoryCard(file, type, repair))
            {
               Console.Error.WriteLine("Failed to write {0}", file);
               Environment.Exit(1);
            }
         }
      };

      if (cmd == "new")
      {
         var cardFileOut = cardFile;
         cardFile = null;

         openCard();

         var type = MemCardType.Raw;
         var repair = false;

         if (args.Length >= 3)
            type = ParseEnumFromUser<MemCardType>(args[2]);
         if (args.Length > 3)
            tooManyArgs();

         if (!card.saveMemoryCard(cardFileOut, (int)type, repair))
         {
            Console.Error.WriteLine("Failed to write {0}", cardFileOut);
            Environment.Exit(1);
         }
      }
      else if (cmd == "list")
      {
         if (args.Length > 2)
            tooManyArgs();

         openCard();

         for (int i=0; i<card.saveType.Length; ++i)
         {
            var type = card.saveType[i];
            if (SaveTypeIsDeleted(type))
               continue;
            Console.Out.WriteLine("{0}: {1}", i, getFilename(i));
         }
      }
      else if (cmd == "export")
      {
         string saveFile = null;
         int index = 0;
         string formatString = null;

         if (args.Length < 3)
            Usage();

         openCard();

         index = findSave(args[2]);
         if (index < 0)
         {
            Console.Error.WriteLine(
               "Could not find save {0} in {1}",
               args[2],
               card.cardLocation
            );
            Usage();
         }

         if (args.Length >= 4)
            saveFile = args[3];
         if (args.Length >= 5)
            formatString = args[4];

         if (args.Length > 5)
            tooManyArgs();

         var format = ParseEnumFromUser<SaveType>(formatString ?? "raw");
         saveFile = saveFile ?? getFilename(index);

         if (!card.saveSingleSave(saveFile, index, (int)format))
         {
            Console.Error.WriteLine("Failed to save {0}", saveFile);
            Environment.Exit(1);
         }
      }
      else if (cmd == "import")
      {
         string saveFile;
         int index;
         int requiredSlots;

         if (args.Length < 4)
            Usage();

         if (args.Length > 4)
            tooManyArgs();

         openCard();

         saveFile = args[2];
         if (!int.TryParse(args[3], out index) ||
             index < 0 ||
             index >= MaxSaves)
         {
            Console.Error.WriteLine("Invalid index {0}", args[2]);
            Usage();
         }

         card.formatSave(index);

         if (!card.openSingleSave(saveFile, index, out requiredSlots))
         {
            string msg;

            if (requiredSlots != 0)
               msg = String.Format(
                  "Not enough space (slots required: {0})",
                  requiredSlots
               );
            else
               msg = "Could not read file";

            Console.Error.WriteLine("Failed to import {0}: {1}", saveFile, msg);
            Environment.Exit(1);
         }

         save();
      }
      else if (cmd == "delete" || cmd == "erase")
      {
         bool erase = (cmd == "erase");

         if (args.Length < 3)
            Usage();

         if (args.Length > 3)
            tooManyArgs();

         openCard();

         int index = findSave(args[2]);
         if (index < 0 || (!erase && SaveTypeIsDeleted(card.saveType[index])))
         {
            Console.Error.WriteLine(
               "Could not find save {0} in {1}",
               args[2],
               card.cardLocation
            );
            Usage();
         }

         if (erase)
            card.formatSave(index);
         else
            card.toggleDeleteSave(index);

         save();
      }
      else if (cmd == "convert")
      {
         if (args.Length < 4)
            Usage();
         if (args.Length > 4)
            tooManyArgs();

         openCard();

         string destFile = args[2];
         var type = ParseEnumFromUser<MemCardType>(args[3]);
         bool repair = false;

         if (!card.saveMemoryCard(destFile, (int)type, repair))
         {
            Console.Error.WriteLine("Failed to write {0}", destFile);
            Environment.Exit(1);
         }
      }
      else
      {
         Usage();
      }
   }

   public static void Main(string [] args)
   {
      try
      {
         MainMain(args);
      }
      catch (Exception e)
      {
         var sb = new StringBuilder();
         sb.Append("Exception: ");
         sb.Append(e.GetType().Name);
         if (e.Message != null && e.Message.Length != 0)
         {
            sb.Append(": ");
            sb.Append(e.Message);
         }
         if (e.StackTrace != null)
         {
            sb.Append("\n");
            sb.Append(e.StackTrace.ToString());
         }
         Console.Error.WriteLine(sb.ToString());
         Environment.Exit(1);
      }
   }
}
