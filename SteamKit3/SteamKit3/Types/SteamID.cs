﻿/*
 * This file is subject to the terms and conditions defined in
 * file 'license.txt', which is part of this source code package.
 */

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace SteamKit3
{
    internal class BitVector64
    {
        private UInt64 data;

        public BitVector64()
        {
        }
        public BitVector64( UInt64 value )
        {
            data = value;
        }

        public UInt64 Data
        {
            get { return data; }
            set { data = value; }
        }

        public UInt64 this[ uint bitoffset, UInt64 valuemask ]
        {
            get
            {
                return ( data >> ( ushort )bitoffset ) & valuemask;
            }
            set
            {
                data = ( data & ~( valuemask << ( ushort )bitoffset ) ) | ( ( value & valuemask ) << ( ushort )bitoffset );
            }
        }
    }

    public class SteamID
    {
        private BitVector64 steamid;

        static Regex SteamIDRegex = new Regex(
            @"STEAM_(?<universe>[0-5]):(?<authserver>[0-1]):(?<accountid>\d+)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase );

        public const uint DesktopInstance = 1;
        public const uint ConsoleInstance = 2;

        public const uint AccountIDMask = 0xFFFFFFFF;
        public const uint AccountInstanceMask = 0x000FFFFF;

        [Flags]
        public enum InstanceFlags : uint
        {
            Clan = ( AccountInstanceMask + 1 ) >> 1,
            Lobby = ( AccountInstanceMask + 1 ) >> 2,
            MMSLobby = ( AccountInstanceMask + 1 ) >> 3,
        }


        public SteamID()
        {
            steamid = new BitVector64();

            AccountID = 0;
            AccountType = EAccountType.Invalid;
            AccountUniverse = EUniverse.Invalid;
            AccountInstance = 0;
        }

        public SteamID( uint unAccountID, EUniverse eUniverse, EAccountType eAccountType )
            : this()
        {
            Set( unAccountID, eUniverse, eAccountType );
        }

        public SteamID( uint unAccountID, UInt32 unInstance, EUniverse eUniverse, EAccountType eAccountType )
            : this()
        {
            InstancedSet( unAccountID, unInstance, eUniverse, eAccountType );
        }

        public SteamID( ulong id )
        {
            SetFromUint64( id );
        }

        public SteamID( string steamId )
            : this ( steamId, EUniverse.Public )
        {
        }

        public SteamID( string steamId, EUniverse eUniverse )
            : this()
        {
            SetFromString( steamId, eUniverse );
        }


        public void Set( UInt32 unAccountID, EUniverse eUniverse, EAccountType eAccountType )
        {
            this.AccountID = unAccountID;
            this.AccountUniverse = eUniverse;
            this.AccountType = eAccountType;

            if ( eAccountType == EAccountType.Clan )
            {
                this.AccountInstance = 0;
            }
            else
            {
                this.AccountInstance = DesktopInstance;
            }
        }

        public void InstancedSet( UInt32 unAccountID, UInt32 unInstance, EUniverse eUniverse, EAccountType eAccountType )
        {
            this.AccountID = unAccountID;
            this.AccountUniverse = eUniverse;
            this.AccountType = eAccountType;
            this.AccountInstance = unInstance;
        }

        public void FullSet( ulong identifier, EUniverse eUniverse, EAccountType eAccountType )
        {
            this.AccountID = ( uint )( identifier & AccountIDMask );
            this.AccountInstance = ( uint )( ( identifier >> 32 ) & AccountInstanceMask );
            this.AccountUniverse = eUniverse;
            this.AccountType = eAccountType;
        }

        public void SetFromUint64( UInt64 ulSteamID )
        {
            this.steamid.Data = ulSteamID;
        }


        public void SetFromString( string steamId, EUniverse eUniverse )
        {
            Match m = SteamIDRegex.Match( steamId );

            if ( !m.Success )
                return;

            uint accId = uint.Parse( m.Groups[ "accountid" ].Value );
            uint authServer = uint.Parse( m.Groups[ "authserver" ].Value );

            this.AccountUniverse = eUniverse;
            this.AccountInstance = 1;
            this.AccountType = EAccountType.Individual;
            this.AccountID = ( accId << 1 ) | authServer;
        }

        public UInt64 ConvertToUint64()
        {
            return this.steamid.Data;
        }

        ulong GetStaticAccountKey()
        {
            return ( ( ulong )AccountUniverse << 56 ) + ( ( ulong )AccountType << 52 ) + AccountID;
        }

        public void CreateBlankAnonLogon( EUniverse eUniverse )
        {
            AccountID = 0;
            AccountType = EAccountType.AnonGameServer;
            AccountUniverse = eUniverse;
            AccountInstance = 0;
        }

        public void CreateBlankAnonUserLogon( EUniverse eUniverse )
        {
            AccountID = 0;
            AccountType = EAccountType.AnonUser;
            AccountUniverse = eUniverse;
            AccountInstance = 0;
        }

        public bool BBlankAnonAccount()
        {
            return this.AccountID == 0 && BAnonAccount() && this.AccountInstance == 0;
        }
        public bool BGameServerAccount()
        {
            return this.AccountType == EAccountType.GameServer || this.AccountType == EAccountType.AnonGameServer;
        }
        public bool BContentServerAccount()
        {
            return this.AccountType == EAccountType.ContentServer;
        }
        public bool BClanAccount()
        {
            return this.AccountType == EAccountType.Clan;
        }
        public bool BChatAccount()
        {
            return this.AccountType == EAccountType.Chat;
        }
        public bool IsLobby()
        {
            return ( this.AccountType == EAccountType.Chat ) && ( ( this.AccountInstance & ( uint )InstanceFlags.Lobby ) != 0 );
        }
        public bool BIndividualAccount()
        {
            return this.AccountType == EAccountType.Individual || this.AccountType == EAccountType.ConsoleUser;
        }
        public bool BAnonAccount()
        {
            return this.AccountType == EAccountType.AnonUser || this.AccountType == EAccountType.AnonGameServer;
        }
        public bool BAnonUserAccount()
        {
            return this.AccountType == EAccountType.AnonUser;
        }
        public bool BConsoleUserAccount()
        {
            return this.AccountType == EAccountType.ConsoleUser;
        }

        public void ClearIndividualInstance()
        {
            if ( !BIndividualAccount() )
                return;

            AccountInstance = 0;
        }

        public bool IsValid()
        {
            if ( this.AccountType <= EAccountType.Invalid || this.AccountType >= EAccountType.Max )
                return false;

            if ( this.AccountUniverse <= EUniverse.Invalid || this.AccountUniverse >= EUniverse.Max )
                return false;

            if ( this.AccountType == EAccountType.Individual )
            {
                if ( this.AccountID == 0 || this.AccountInstance > ConsoleInstance )
                    return false;
            }

            if ( this.AccountType == EAccountType.Clan )
            {
                if ( this.AccountID == 0 || this.AccountInstance != 0 )
                    return false;
            }

            return true;
        }

        public UInt32 AccountID
        {
            get
            {
                return ( UInt32 )steamid[ 0, 0xFFFFFFFF ];
            }
            set
            {
                steamid[ 0, 0xFFFFFFFF ] = value;
            }
        }

        public UInt32 AccountInstance
        {
            get
            {
                return ( UInt32 )steamid[ 32, 0xFFFFF ];
            }
            set
            {
                steamid[ 32, 0xFFFFF ] = ( UInt64 )value;
            }
        }

        public EAccountType AccountType
        {
            get
            {
                return ( EAccountType )steamid[ 52, 0xF ];
            }
            set
            {
                steamid[ 52, 0xF ] = ( UInt64 )value;
            }
        }

        public EUniverse AccountUniverse
        {
            get
            {
                return ( EUniverse )steamid[ 56, 0xFF ];
            }
            set
            {
                steamid[ 56, 0xFF ] = ( UInt64 )value;
            }
        }

        public string Render()
        {
            switch ( AccountType )
            {
                case EAccountType.Invalid:
                case EAccountType.Individual:
                    if ( AccountUniverse <= EUniverse.Public )
                        return String.Format( "STEAM_0:{0}:{1}", AccountID & 1, AccountID >> 1 );
                    else
                        return String.Format( "STEAM_{2}:{0}:{1}", AccountID & 1, AccountID >> 1, ( int )AccountUniverse );
                default:
                    return Convert.ToString( this );
            }
        }

        public override string ToString()
        {
            return Render();
        }

        public static implicit operator UInt64( SteamID sid )
        {
            return sid.steamid.Data;
        }

        public static implicit operator SteamID( UInt64 id )
        {
            return new SteamID( id );
        }

        public override bool Equals( System.Object obj )
        {
            if ( obj == null )
                return false;

            SteamID sid = obj as SteamID;
            if ( ( System.Object )sid == null )
                return false;

            return steamid.Data == sid.steamid.Data;
        }

        public bool Equals( SteamID sid )
        {
            if ( ( object )sid == null )
                return false;

            return steamid.Data == sid.steamid.Data;
        }

        public static bool operator ==( SteamID a, SteamID b )
        {
            if ( System.Object.ReferenceEquals( a, b ) )
                return true;

            if ( ( ( object )a == null ) || ( ( object )b == null ) )
                return false;

            return a.steamid.Data == b.steamid.Data;
        }

        public static bool operator !=( SteamID a, SteamID b )
        {
            return !( a == b );
        }

        public override int GetHashCode()
        {
            return steamid.Data.GetHashCode();
        }

    }
}