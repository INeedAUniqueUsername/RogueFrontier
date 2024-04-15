using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using LibGamer;
using Newtonsoft.Json;
using G = RogueFrontier.Timeline.GetDockScreen;
namespace RogueFrontier;

public interface IPlayerInteraction {
	IScene GetScene (IScene prev, PlayerShip playerShip, IDockable d);
}
public class IntroMeeting : IPlayerInteraction {
    Timeline story;
    public IntroMeeting(Timeline story) {
        this.story = story;
    }
    public IScene GetScene (IScene prev, PlayerShip playerShip, IDockable d) {
        var t = (d as Station)?.type;
        if(t?.codename != "station_daughters_outpost")
            return null;
        var heroImage = t.HeroImage;
        return Intro();
        Dialog Intro() {
            var t =
@"Docking at the front entrance of the abbey, the great
magenta tower seems to reach into the oblivion above
your head. It looks much more massive from the view of
the platform that juts out the side of the docking ring
The rows of stained glass windows glow warmly with
orange light. Nevertheless, you can't help but think...

You are a complete stranger here.";
            var sc = new Dialog(t, [
                new("Continue", Intro2),
                new("Leave", Intro2b, NavFlags.ESC)
            ]) { background = heroImage };
            return sc;
        }
		Dialog Intro2 (IScene from) {
            var t =
@"Walking into the main hall, You see a great monolith of
sparkling crystals and glowing symbols. A low hum reflectes
throughout the room. If you stand still, you can hear
some indistinct whispering from your side.

A stout guard stands for reception duty next to a gate.

""Ah, hello! A gathering is currently in session.
You must be new here - We are currently listening
for the Galactic Song, a magical harmony unlike
any other. We may also receive a message from
The Orator today!"" the guard says.";
            var sc = new Dialog(t, [
                new(@"""Ummm, yeah, The Orator?""", Intro3)
            ]) { background = heroImage };
            return sc;
        }
		Dialog Intro2b (IScene from) {
            var t =
@"You decide to step away from the station,
much to the possible chagrin of some mysterious
entity and several possibly preferred timelines.".Replace("\r", null);
            return new (t, [
                new("Undock")
            ]) { background = heroImage };
        }
		Dialog Intro3 (IScene from) {
            var t =
@"""Ummm, yeah, The Orator?""

The guard replies:

""From Pericles to Ston, yes, it is The Orator we hear!
The Orator tells us truth and truth only, and grants us
the power to break silences. But interestingly, to hear
The Orator also requires holding silence. So, to be a
good Listener means knowing the right time to speak!""";
            var sc = new Dialog(t, [
                new(@"""Well, The Orator told me...""", Intro4)
            ]) { background = heroImage };
            return sc;
        }
		Dialog Intro4 (IScene from) {
            string t =
@"""Well, The Orator told me...
that there is something terribly wrong
happening to us. All of us. Humanity.

Forces of conflict are coming from the Celestial Center
and causing some horrible wars throughout our civilization.
A voice known as The Dictator seeks to control us in horrible ways.

I asked The Orator about what The Dictator's intentions were, and then
I heard a droning voice begin to speak loudly over The Orator, slowly
raising itself into a dreadful yell. My senses began falling apart.
One voice shouted ""SILENCE"" at the other Voice and both went quiet,
then The Orator spoke a final message.

The Orator told me... that They had an answer. And that
if I went to Them via journey to the Celestial Center,
and I listened to Their words, and I wielded Their powers,
then... They would bring forth an ultimate peace.

And...""

Wait, how are you saying all of this- Your mind blanks out.

""...And I... I witnessed all of this in a strange dream I had.""";
            var sc = new Dialog(t, [
                new("Continue", Intro5)
            ]) { background = heroImage };
            return sc;
        }
		Dialog Intro5 (IScene from) {
            string t =
@"The guard replies:

""...I see. So you're one of those who were called to
leave Human Space. I remember having one dream that
was just like that.""

""In that dream, I could see everything - horrible battles,
ships pulverized by plasma, stations destroyed by missiles,
in distant star systems I could never even fathom visiting.
But I was terribly fearful of the Dictator - so fearful that
I vowed to see the Celestial Center and personally destroy
everything that I could find of The Dictator myself.""

""But then I figured that it was just a dream, not a destiny.
Now I know of older followers who understand that leaving
for the Celestial Center is not the only answer.""

""The old survivors built this place to provide a shelter
for those who seek a different kind of answer - one that
values peace from within. Our bond with The Orator grants
us safety from The Dictator. As a disciple of the
Daughters of the Orator, I welcome you to reside here.""

""Unless, your answer rests..."" he points to a distant star
shining through the window, ""...far out there.""

""Does it?""";
            var sc = new Dialog(t, [
                new(@"""It does.""", Intro6, NavFlags.ESC)
            ]) { background = heroImage };
            return sc;
        }
		Dialog Intro6 (IScene from) {
            string t =
@"
After a long pause, you respond.

""It does.""

The guard thinks for a minute.

""I figured. You have your own starship, fit for
leaving this system and exploring the stars beyond.
We don't really see modern builds like yours
around here... Not since the last war ended.

You really intend to see what's out there.""";
            return new(t, [
                new(@"""That is correct.""", Intro7, NavFlags.ESC)
            ]) { background = heroImage }; ;
        }
		Dialog Intro7 (IScene from) {
            string t =
@"He looks at you with doubt.

""So you understand that... this? This is not
the first time that The Orator has spoken,
and told someone to just pack up, leave,
and look for Them somewhere out there?

I don't care how ready you think you are
to go wherever you think you're going.

Are you prepared to die?""";
            return new(t, [
                new(@"""Huh?!?!?!""", Intro8)
            ]) { background = heroImage };
        }
		Dialog Intro8 (IScene from) {
            string t =
@"He looks somewhat tense.

""The Orator definitely calls to people. We know that this
happens occasionally but predictably. We see a new person
come in for their first time, and ask us about The Orator,
and, and,

It's only a matter of days until they leave this place
for the last time...

...And we never see that person again.

Until they show up in a news report in which
someone identifies them as an unwitting traveler
who got blown up in the middle of a war zone...

You'd have better chances of surviving if you joined
the Constellation Fleet. Not much better, mind you.
At least I got out when they were about to send us
through the gateway... before they'd shut the door
and lock it behind us.

So, tell me, what is it that you intend to do?""";
            return new Dialog(t, [
                new(@"""I intend to reach the Celestial Center.""", Intro9a),
                new("...", Intro9b)
            ]) { background = heroImage };
        }
		Dialog Intro9a (IScene from) {
			story.mainInteractions.Remove(this);
            string t =
@"The man sighs and stares at the ground for a second.

""So you do. Okay. I won't try to change your mind.
Just remember...""";
            return new(t, [
                new("Continue", Intro11)
            ]) { background = heroImage };
        }
		Dialog Intro9b (IScene from) {
			story.mainInteractions.Remove(this);
            string t =
@"You pause for a moment.";
            t = t.Replace("\r", null);
            return new(t, [
                new('I', @"""I intend to reach the Celestial Center.""", Intro10a),
            ]) { background = heroImage }; ;
        }

		Dialog Intro10a (IScene prev) {
			story.mainInteractions.Remove(this);
            string t =
@"""You sound uncertain there.
Do you truly intend to do that?""";
            return new(t, [
                new('I', @"I intend to reach the Celestial Center.", Intro9a),
            ]) { background = heroImage };
        }
		Dialog Intro11 (IScene prev) {
			story.mainInteractions.Remove(this);
            string t =
@"""You will meet many different friends and foes
during your journey. Especially on the frontier,
you might be surprised at the kind of people that
you meet. And there are some who will want to
destroy you.

And with your starship, you may choose how much
trouble you want to involve yourself in. We, the
Daughters of the Orator, do not have any particular
opinion on how you ought to conduct yourself, but
we advise you to do only what feels right to you.

Please, please, do not abuse whatever magical powers
The Orator has given to you. We, the Daughters of the
Orator, have seen enough of that happen.

That is all.""";
            t = t.Replace("\r", null);
            return new(t, [
                new('C', @"Undock", Done),
            ]) { background = heroImage };
        }
		Dialog Done (IScene prev) {
			story.mainInteractions.Remove(this);
            return null;
        }
    }
}
class IntroTraining : IPlayerInteraction {
    Timeline story;
    Station station;
    public AIShip[] drones;
    public int startTick;
    public IntroTraining(Timeline story, Station station, PlayerShip player) {
        this.story = story;
        this.station = station;
        var w = station.world;
        var shipClass = w.types.Lookup<ShipClass>("ship_laser_drone");
        var sovereign = Sovereign.SelfOnly;
        this.drones = new AIShip[3];
        var k = station.world.karma;
        for (int i = 0; i < 3; i++) {
            var d = new AIShip(new(w, shipClass, station.position + XY.Polar(k.NextDouble() * 2 * Math.PI, k.NextDouble() * 25 + 25)),
                sovereign, new SnipeAt(player));
            drones[i] = d;
        }
    }
    public void AddDrones() {
        foreach (var d in drones) {
            station.world.AddEntity(d);
            station.world.AddEffect(new Heading(d));
        }
    }
    public IScene GetScene (IScene prev, PlayerShip playerShip, IDockable d) {
        if (d == station) {
            var s = station;
            var heroImage = s.type.HeroImage;
            var count = drones.Count(d => d.active);
            if (count > 0) {
                return InProgress();
            } else {
                return Complete();
            }
            Dialog InProgress() {
                var t =
@$"Benjamin meets you at the docking bay.

""There's still {count} drones left.""
".Replace("\r", null);
                return new(t, [
                    new("Continue")
                ]) { background = heroImage };
            }
			IScene Complete () {
                var sec = (station.world.tick - startTick) / 60;
                var t =
@$"Benjamin meets you at the docking bay.

""You destroyed the drones in {sec} seconds.""

{(sec < 60 ?
    @"""I figured you were ready for that.""" :
    @"""So now you should know how to aim.""")}";
                var sc = new Dialog(t, [
                    new("Continue", Explore)
                ]) { background = heroImage };
                return sc;
            }

			IScene Explore (IScene prev) {

                var t =
@$"""There are lots of people - friendly or unfriendly - out there.
In a place like this, you'll find plenty of  stations offering trade
and services for money. Some might have jobs that you can take.""

""Others will attack you on sight. Be careful around them.""

""Only time will tell who is who.""

""Take a look around this system and find out who can help you.""";
                var sc = new Dialog(t, [
                    new("Undock", Undock)
                ]) { background = heroImage };
                return sc;
            }
			IScene Undock (IScene prev) {
				story.mainInteractions.Remove(this);
				story.mainInteractions.Add(new IntroExploration(story, station, playerShip));
                return null;
            }
        }
        return null;
    }
}
class IntroExploration : IPlayerInteraction {
    Timeline story;
    Station station;
    HashSet<Station> targets;
    public IntroExploration(Timeline story, Station station, PlayerShip playerShip) {
        this.story = story;
        this.station = station;
        var w = station.world;
        targets = new HashSet<Station>(w.entities.all.OfType<Station>().Where(
            s => s.sovereign.IsFriend(playerShip)));
    }
    public IScene GetScene (IScene prev, PlayerShip playerShip, IDockable d) {
        var s = station;
        if (d != s) return null;

        var heroImage = s.type.HeroImage;
        int c = targets.Count - targets.Intersect(playerShip.known).Count();
        if (c > 0) {
            return new Dialog(
@$"""You've found all but {c} friendly station{(c > 1 ? "s" : "")} in this system.
Use your starship's megamap to look for them.""",
                [
                    new("Undock")
                ]) { background = heroImage };
        } else {

            return new Dialog(
@$"""You've found all the friendly stations in the system. Now that
you know what services each provide you can return to them when
needed.""",
            [
                new("Continue", Continue)
            ]) { background = heroImage };
			IScene Continue (IScene prev) {
                return new Dialog(
@$"""Now that you're learning to find what you need,
let me give you another goal.""

""Look around the system. Find all the warlords and pirates,
where they're hiding in this system.""

""Then, look out there, far out there. At the edge of the system
is an Errorist compound, where unhinged ""scientists"" commit
inhumane experiments involving radiation.""

""Fight back at the Orion Warlords and Iron Pirates.
And when you've gotten used to fighting warlords and pirates,
go and start destroying Errorists.""",
                [
                    new("Continue", Undock)
                ]) { background = heroImage };
            }
			IScene Undock (IScene prev) {
				story.mainInteractions.Remove(this);
				story.mainInteractions.Add(new IntroOuterEnemy(story, station, playerShip));
                return null;
            }
        }
    }
}
class IntroOuterEnemy : IPlayerInteraction {
    Timeline story;
    Station station;
    Station target;
    public IntroOuterEnemy(Timeline story, Station station, PlayerShip playerShip) {
        this.story = story;
        this.station = station;
        var w = station.world;

        target = new Station(w, w.types.Lookup<StationType>("station_errorist_compound"), XY.Polar(1, 800));
        target.CreateSegments();
        target.CreateGuards();
        w.AddEntity(target);
    }
    public IScene GetScene (IScene prev, PlayerShip playerShip, IDockable d) {
        if (d != station) {
            return null;
        }
        var s = station;
        var heroImage = s.type.HeroImage;

        if (target.active) {

            return new Dialog(
@$"""Go and destroy the Errorist compound when you're ready.""",
                [new("Undock")]) { background = heroImage };
        } else {
            var sc = new Dialog(
@$"""Well, congratulations. You've survived my final exam.
That's all the training I have for you. Hopefully now you
have at least a fighting chance when you leave this place.""

""Goodbye.""",
                [new("Continue", Undock)]) { background = heroImage };
			IScene Undock (IScene prev) {
				story.mainInteractions.Remove(this);
                return null;
            }
            return sc;
        }
    }
}
record InvestigateBeowulfClub(Station dest) {
    public IScene GetScene (IScene prev, PlayerShip playerShip, IDockable d) {
        if(d == dest) {
            return new Dialog(
@"You are docked at an independent chapter
of the Beowulf Club, a galaxy-wide alliance
of civilian gunship pilots.

A heavily armored guard calls out to you.
""Hey! Who do you think you are with that
brittle piece of junk? Get outta here!""", [
                new("Respond", A2)
            ]);


			IScene A2 (IScene prev) {
                return new Dialog(
@"You say:

""The Constellation Militia has received
reports of weapons trafficking happening
on this station. I am here to investigate.""
",                          [
                    new("Continue", A3)
                ]);
            }
			IScene A3 (IScene prev) {
                playerShip.GiveItem("item_specrom_magic_blaster_i");
                return new Dialog(
@"The guard slowly approaches you:

""Oh, I'm sorry. The Beowulf Club is a most
*distinguished* organization. We can assure
you that this station of ours does not and
never will harbor any weapons trafficking.
We are merely civilians trying to protect
ourselves from warlords and magical-wielding
megalomaniacs on our own terms.""

The guard grabs your shoulder and whispers:
""Let me make you aware of some useful information.

It's dangerous for a little ship like yours
to go alone out there. So here, have this...""

You receive [SpecROM: Magic Blaster I]

""...And please, let the Militia know that
there is nothing to worry about here.""", new() {
                new("Undock", null)
});
            }
        }
        return null;
    }
}
public static class SPlayerStory {
    public static bool IsAmethyst(this Item i) => i.HasAtt("Amethyst");

    public static void GiveItem(this PlayerShip pl, string type) =>
        pl.cargo.Add(new(pl.world.types.Lookup<ItemType>(type)));
}
//TO DO: update crawl backgrounds
/*  
"If you ever encounter a Perfectron, just *run* away.
They are mad with weaponry and will destroy whatever they please.
There's a reason why we never agree to negotiate with them.
*/
public class Timeline : Ob<EntityAdded>, Ob<Station.Destroyed>, Ob<AIShip.Destroyed> {
    public HashSet<IPlayerInteraction> mainInteractions;
    public HashSet<IPlayerInteraction> secondaryInteractions;
    public HashSet<IPlayerInteraction> completedInteractions;
    Dictionary<ItemType, int> stdPrice;
    public int GetStdPrice(Item i) => stdPrice.TryGetValue(i.type, out var v) ? v : 0;

    G GetDock(string s) => s switch {
        "station_daughters_outpost" => DaughtersOutpost,

        "station_constellation_astra" => ConstellationAstra,
        "station_constellation_habitat" => ConstellationVillage,

        "station_armor_shop" => ArmorDealer,
        "station_arms_dealer" => ArmsDealer,

        "station_amethyst_store" => AmethystStore,
        "station_beowulf_club" => BeowulfClub,
        "station_camper_outpost" => CamperOutpost,

        "station_orion_warlords_camp" => OrionWarlordsCamp,
        _ => null
    };
    Dictionary<string, int> stdPriceTable => new {
        item_amethyst_laser_i = 4000,
        item_amethyst_laser_ii = 6000,
        item_shimmer_shield_i = 800,
        item_shimmer_shield_ii = 1200,
        item_gemsteel_plate_i = 2000,
        item_gemsteel_plate_ii = 3000,
        item_radiant_plate = 3200,
        item_sand_cannon = 2000,
        item_sludge_cannon = 3600,
        item_iron_driver = 3000,
        item_iron_cannon = 5000,
        item_ironclad_plate = 3600,
        item_ironside_plate = 4000,
        item_grinder = 4000,
        item_deflect_device = 4000,
        item_lightning_cannon = 3500,
        item_orion_bolter = 400,
        item_orion_longbow = 800,
        item_orion_ballista = 1200,
        item_orion_turret = 900,
        item_orion_sentry = 2700,
        item_orion_skewer = 2700,
        item_hunterscale_plate = 250,
        item_skullhelm_plate = 600,
        item_dark_cannon = 3000,
        item_thorn_missile = 16,
        item_thorn_missile_system = 3200,
        item_simple_fuel_rod = 50,
        item_armor_repair_patch = 200,
        item_amethyst_repair_kit_i = 400,
        item_amethyst_repair_kit_ii = 800,
        item_orator_charm_silence = 3000,
        item_dictator_charm_silence = 3000,
        item_emp_cannon = 2400,
        item_tracking_laser = 600,
        item_scanning_laser = 600,
        item_beowulf_dual_laser_cannon = 3000,
        item_beowulf_dual_laser_repeater = 3000,
        item_beowulf_dual_laser_upgrade = 3000,
        item_buckler_shield = 400,
        item_klaw_missile = 8,
        item_klaw_missile_launcher = 400,


        item_fang_missile = 20,
        item_fang_missile_launcher = 1000,

        item_musket_turret = 3500,
        item_sidearm_turret = 3500,
        item_missile_defender = 4000,
        item_scanner_drone = 1500,
        item_combat_drone_i = 4000,
        item_flintlock = 1500,
        item_sabre = 2500,
        item_knightsteel_plate = 5000,
        item_bumpersteel_plate = 4500,
        item_bandit_plate = 8000,
        
        item_dynamite_charge = 12,
        item_dynamite_cannon = 2000,
        item_amethyst_member_card = 200,
        item_shield_bash = 3000,
        item_20mw_generator = 5000,
        item_20mw_solar = 500,

        item_amethyst_25mw_generator = 6000,
        item_amethyst_50mw_generator = 12000,
        item_magic_blaster_i = 2000,
        item_magic_blaster_ii = 3000,
        item_amethyst_laser_iii = 8000,
        item_light_launcher_i = 2500,
        

        item_dagger_cannon = 3500,
        item_shrapnel_bomb = 12,
        item_bomb_launcher = 2500,
        item_metal_grinder = 6000,
        item_cloaking_shield = 2400,
        item_darkened_knightsteel_plate = 7500,


        item_magic_bomb = 15,
        item_magic_bomb_launcher = 2500,

        item_sand_blaster = 1600,
        item_sand_vent = 2400,
        item_sludge_vent = 2400,
        item_demon_cannon = 6000,

        item_hull_puncher = 6000,
        item_iron_hook_cannon = 3200,
        item_iron_hook = 10,
        
        item_flashbang_cannon = 4800,
        item_lightning_vent = 2400,
        item_shining_armor = 6400,
        
        item_nova_missile = 24,
        item_nova_missile_launcher = 4800,

        item_10mw_loneheart = 8000,
        item_20mw_primary = 8000,

        item_50mw_generator = 12000,
        item_10mw_secondary = 8000,
        item_20mw_secondary = 16000,
        item_30mw_secondary = 24000,
        item_40mw_secondary = 32000,
        item_50mw_secondary = 40000,
        item_60mw_secondary = 48000,

        item_prescience_book = 1999,
        item_book_founders = 1999,
        
        item_shine_charm = 5000,
        item_gem_of_monologue = 500,
        
        item_repeater_turret = 9000,
        item_magic_shotgun_i = 1500,

        item_dark_magic_blaster = 25000,
        item_dark_lightning_cannon = 25000,

        item_bronze_rice = 600,
        item_biocart_transcendence = 900,

        item_flakbang_cannon = 4800,
        item_tipped_orion_longbow = 4800,

        item_specrom_magic_blaster_i = 8400,

        item_debug_missile = 0,
        item_debug_missile_launcher = 0
    }.ToDict<int>();
    public Timeline(PlayerShip playerShip) {
        var i = playerShip.world.types.GetDict<ItemType>();
        stdPrice = stdPriceTable.ToDictionary(pair => i[pair.Key], pair => pair.Value);
        var missing = i.Keys.Except(stdPriceTable.Keys).ToList();
        if (missing.Any()) {
            throw new Exception(string.Join('\n', missing.Select(m => @$"[""{m}""] = 0,")));
        }
        mainInteractions = [new IntroMeeting(this)];
        secondaryInteractions = [];
        completedInteractions = [];
        var univ = playerShip.world.universe;
        foreach (var e in univ.GetAllEntities()) {
            Register(e);
        }
        univ.onEntityAdded += this;
    }
    public void Observe(EntityAdded ev) => Register(ev.e);
    public void Register(Entity e) {
        switch(e) {
            case Station s: s.onDestroyed += this; break;
            case AIShip a: a.onDestroyed += this; break;
        }
    }
    public void Observe(Station.Destroyed ev) {
    
    }
    public void Observe(AIShip.Destroyed ev) {
    
    }
    public void Update(PlayerShip playerShip) {

    }
    public delegate IScene GetDockScreen(IScene prev, PlayerShip playerShip, Station source);
    public IScene GetScene(IScene prev, PlayerShip p, IDockable d) {
        return
			mainInteractions.Select(m => m.GetScene(prev, p, d)).FirstOrDefault(s => s != null) is RogueFrontier.IScene c ?
                c :
            d is Station st && GetDock(st.type.codename) is G g ?
                g(prev, p, st) :
            null;
    }
    public IScene AmethystStore (IScene prev, PlayerShip playerShip, Station source) {
        var discount = playerShip.cargo.Any(i => i.type.codename == "item_amethyst_member_card");
        var buyAdj = discount ? 0.8 : 1;
        return
            CheckConstellationArrest(prev, playerShip, source) ??
            Intro();
        Dialog Intro() {
            return new(
@$"You are docked at The Amethyst Store,
one of several commercial stations
established by Amethyst, Inc to serve
all your Amethyst-related needs,
including but not limited to, product
purchases and repair services.

{(discount ?
"You scan your Amethyst membership card" +
"at the entrance to receive discounts on" +
"maintenance and upgrades at this station."
: "")}
",
            [
                new("Trade", Trade),
                SNav.DockDeviceInstall(playerShip, GetInstallPrice),
                SNav.DockDeviceRemoval(playerShip, GetRemovePrice),
                SNav.DockArmorRepair(playerShip, GetRepairPrice),
                SNav.DockArmorReplacement(playerShip, GetReplacePrice),
                new("Undock")
            ]);
        }
        int GetRepairPrice(Armor a) =>
            !a.source.IsAmethyst() ? -1 :
            discount ? 1 :
            3;
		IScene Trade (IScene from) => new TradeMenu(from, playerShip, source,
            (object i) => (int)(GetStdPrice(i) * buyAdj),
            (object i) => i.IsAmethyst() ? GetStdPrice(i) / 10 : -1);
        int GetInstallPrice(Device d) =>
            !d.source.IsAmethyst() ? -1 : discount ? 80 : 100;
        int GetRemovePrice(Device i) =>
            !i.source.IsAmethyst() ? -1 : discount ? 80 : 100;
        int GetReplacePrice(Device i) =>
            !i.source.IsAmethyst() ? -1 : discount ? 80 : 100;
    }
    public IScene BeowulfClub (IScene prev, PlayerShip playerShip, Station source) {
        if (!playerShip.shipClass.attributes.Contains("BeowulfClub")) {
            return new Dialog(
@"You are docked at an independent chapter
of the Beowulf Club, a galaxy-wide alliance
of civilian gunship pilots.

A heavily armored guard calls out to you.
""Hey! Who do you think you are? Get your
piece of junk off of this station!""", [new("Undock immediately")]);
        }
        return Intro();
        Dialog Intro() {
            return new(
@"You are docked at an independent branch
of the Beowulf Club, a galaxy-wide alliance
of civilian gunship pilots.",
            [
                new("Trade", Trade),
                SNav.DockArmorRepair(playerShip, GetArmorRepairPrice),
                SNav.DockArmorReplacement(playerShip, GetArmorReplacePrice),
                SNav.DockDeviceInstall(playerShip, GetDeviceInstallPrice),
                SNav.DockDeviceRemoval(playerShip, GetDeviceRemovalPrice),
                new("Undock")
            ]);
        }
        int GetArmorRepairPrice(Armor a) {
            if (a.source.IsAmethyst()) {
                return 6;
            }
            return 3;
        }
        int GetArmorReplacePrice(Armor a) {
            return 100;
        }
        int GetDeviceInstallPrice(Device d) {
            return 100;
        }
        int GetDeviceRemovalPrice(Device d) {
            return 100;
        }
		IScene Trade (IScene from) => new TradeMenu(from, playerShip, source,
            GetStdPrice,
            (object i) => GetStdPrice(i) / 4);
    }
    public IScene CamperOutpost (IScene prev, PlayerShip playerShip, Station source) {
        var lookup = playerShip.world.types.Lookup<ItemType>;
        var recipes = new Dictionary<string, Dictionary<string, int>> {
            ["item_orion_longbow"] = new() {
                ["item_orion_bolter"] = 4
            },
            ["item_magic_shotgun_i"] = new() {
                ["item_magic_blaster_i"] = 1,
                ["item_dynamite_charge"] = 30,
            }
        }.ToDictionary(
            pair => lookup(pair.Key),
            pair => pair.Value.ToDictionary(
                pair => lookup(pair.Key),
                pair => pair.Value));
        return Intro();
        Dialog Intro() {
            return new(
@"You are docked at a Campers Outpost,
an independent enclave of tinkers,
craftspersons, and adventurers.",
            [
                new("Trade", Trade),
                new("Workshop", Workshop),
                SNav.DockArmorRepair(playerShip, GetArmorRepairPrice),
                SNav.DockArmorReplacement(playerShip, GetArmorReplacePrice),
                SNav.DockDeviceInstall(playerShip, GetDeviceInstallPrice),
                SNav.DockDeviceRemoval(playerShip, GetDeviceRemovalPrice),
                new("Undock")
            ]);
        }
        int GetArmorRepairPrice(Armor a) => a.source.IsAmethyst() ? -1 : 2;
        int GetArmorReplacePrice(Armor a) => a.source.IsAmethyst() ? -1 : 100;
        int GetDeviceInstallPrice(Device a) => a.source.IsAmethyst() ? -1 : 100;
        int GetDeviceRemovalPrice(Device a) => a.source.IsAmethyst() ? -1 : 100;

		IScene Trade (IScene from) => new TradeMenu(from, playerShip, source,
            GetStdPrice,
            (object i) => GetStdPrice(i) / 4);
		IScene Workshop (IScene from) => SMenu.Workshop(from, playerShip, recipes, null);
    }
    public TradeMenu TradeStation(IScene prev, PlayerShip playerShip, Station source) =>
        new (prev, playerShip, source, GetStdPrice, i => GetStdPrice(i) / 2);

    public IScene CheckConstellationArrest (IScene prev, PlayerShip playerShip, Station source) {
        return GetConstellationCrimes(playerShip, source).FirstOrDefault() is ICrime c ?
            ConstellationArrest(prev, playerShip, source, c) : null;
    }
    public IScene ConstellationArrest (IScene prev, PlayerShip playerShip, Station source, ICrime c) {
        return new Dialog(
@"Constellation soldiers approach your ship
as you dock.",
            [
                new("Continue docking", Arrest),
                new("Cancel", Cancel)
            ]);
		IScene Arrest (IScene prev) {
            return new Dialog(
@$"The soldiers storm your ship and restrain
you on the ground before you can invoke SILENCE.

""You are under immediate arrest for
{c.name}.""",
            [new("Continue", Death)]
            );
        }
		IScene Cancel (IScene prev) {
            if (playerShip.world.karma.NextDouble() < 1) {
                if(source.behavior is ConstellationAstra c) {
                    foreach(var g in c.reserves.Take(playerShip.world.karma.NextInteger(4, 8)).ToList()) {
                        playerShip.world.AddEntity(g);
                        c.reserves.Remove(g);
                        (g.behavior.GetOrder() as GuardAt)?.SetAttack(playerShip, -1);
                    }
                }
                source.guards.ForEach(s => (s.behavior.GetOrder() as GuardAt)?.SetAttack(playerShip, -1));
            }
            return null;
        }
		IScene Death (IScene prev) {
            playerShip.Destroy(source);
            return null;
        }
    }
    public IEnumerable<ICrime> GetConstellationCrimes(PlayerShip p, Station source) {
        return p.crimeRecord.Where(c => c is DestructionCrime d
            && ReferenceEquals(d.destroyed.sovereign, source.sovereign)
            && !d.resolved);
    }
    public IScene ArmorDealer (IScene prev, PlayerShip playerShip, Station source) {
        return CheckConstellationArrest(prev, playerShip, source) ??
            new Dialog(
@"You are docked at an armor shop station.", new() {
            new("Trade: Armor", Trade),
            new("Undock")
        }) { background = source.type.HeroImage };
        TradeMenu Trade(IScene c) =>
            new(c, playerShip, source, GetStdPrice, (object i) => i.armor is Armor a ?
                    (int)(a.valueFactor * 0.5 * GetStdPrice(i)) :
                    -1);
    }
    public IScene ArmsDealer (IScene prev, PlayerShip playerShip, Station source) {
        return CheckConstellationArrest(prev, playerShip, source) ?? 
            new Dialog(
@"You are docked at an arms dealer station", new() {
            new("Trade: Weapons", Trade),
            new("Undock")
        }) { background = source.type.HeroImage };
        TradeMenu Trade(IScene c) =>
            new(c, playerShip, source, GetStdPrice, (object i) => i.weapon is Weapon w ?
                (int)(w.valueFactor * 0.5 * GetStdPrice(i)) :
                -1);
    }
    public HashSet<IShip> militiaRecordedKills = [];
    public bool constellationMilitiaMember; 
    public IScene ConstellationAstra (IScene prev, PlayerShip playerShip, Station source) {
        var friendlyOnly = ItemFilter.Parse("-OrionWarlords -IronPirates -Errorists -Perfectrons -DarkStar");
        return CheckConstellationArrest(prev, playerShip, source) ??
            Intro(prev);
		Dialog Intro (IScene prev) {
            return new(
@"You are docked at a Constellation Astra,
a major residential and commercial station
of the United Constellation.

The station is a stack of housing units,
utility-facilities, entertainment districts,
business sectors, and trading rooms. The governing
tower protrudes out the ceiling of the station.
The rotator tower rests on the underside.
From a distance, the place looks almost like
a spinning pinwheel.

There is a modest degree of artificial gravity here.",
            [
                new("Trade", Trade),
                SNav.DockDeviceInstall(playerShip, GetInstallPrice),
                SNav.DockDeviceRemoval(playerShip, GetRemovePrice),
                SNav.DockArmorRepair(playerShip, GetRepairPrice),
                SNav.DockArmorReplacement(playerShip, GetReplacePrice),
                new("Militia Headquarters", MilitiaHeadquarters),
                new("Undock")
            ]) { background = source.type.HeroImage };
        }
		IScene Trade (IScene from) =>
            new TradeMenu(from, playerShip, source, GetStdPrice,
                (object i) => (!i.HasDevice() || friendlyOnly.Matches(i)) ? GetStdPrice(i) / 5 : -1
                );
        int GetRepairPrice(Armor a) => !friendlyOnly.Matches(a.source) ? -1 : a.source.IsAmethyst() ? 9 : 3;
        int GetInstallPrice(Device d) => !friendlyOnly.Matches(d.source) ? -1 : d.source.IsAmethyst() ? 300 : 100;
        int GetRemovePrice(Device d) => !friendlyOnly.Matches(d.source) ? -1 : d.source.IsAmethyst() ? 300 : 100;
        int GetReplacePrice(Device d) => !friendlyOnly.Matches(d.source) ? -1 : d.source.IsAmethyst() ? 300 : 100;
		IScene MilitiaHeadquarters (IScene from) {
            if (!constellationMilitiaMember) {
                return new Dialog(
@"You enter the lobby area before the
Militia Headquarters. The room is mostly
empty, save for a kiosk where civilians
may sign a military service constract.

""The Constellation Militia now offers a new
Citizens' Defense program in which you shall
receive reimbursement for combat against ships
and stations belonging to criminal factions.

The criminal factions are listed as follows:
- Orion Warlords
- Iron Pirates

Note that for security purposes, your
ship will be fitted with a tracking device.

Membership shall be revoked if you are found
guilty of any criminal activity.

To join, please sign your name below.""",
                    [
                        new("Sign", Sign),
                        new("Decline", Intro)
                    ]);
				IScene Sign (IScene from) {
					constellationMilitiaMember = true;
					militiaRecordedKills.UnionWith(playerShip.shipsDestroyed);
                    return new Dialog(
@"You have joined the Citizens' Defense program.

Thank you for doing your part in maintaining
our collective security.",
                        [
                            new("Continue", Rookie)
                        ]);
                }
				IScene Rookie (IScene from) {
                    return new Dialog(
@"""Now *before* you run off...""

An unseen officer abruptly chimes in
from behind, startling you.

""Don't get ahead of yourself, rookie.
Around these stars, you could easily
find yourself outnumbered by baddies
if you even think of running against
some little warlord's camp head-on.

People do not play nice around here.

Nobody does.

Get back here in one hull, alright?
You don't want to know what they do to
people like you...""

The officer points to a distant star
on their tablet showing a tactical map.

""...far out there. Got it?""",
                        [
                            new("Continue", MilitiaHeadquarters)
                        ]);
                }
            } else {
                var rewardTable = (new {
                    ship_orion_raider = 20,
                    ship_orion_huntsman = 40,
                    ship_iron_gunboat = 40,
                    ship_iron_missileship = 80,
                    ship_iron_destroyer = 60,
                    ship_iron_frigate = 80
                }).ToDict<int>();
                var groups = playerShip.shipsDestroyed
                    .Except(militiaRecordedKills)
                    .GroupBy(ship => ship.shipClass)
                    .Select(g => (
                        name: g.Key.name,
                        count: g.Count(),
                        payment: rewardTable.TryGetValue(g.Key.codename, out var unitValue) ? unitValue * g.Count() : 0
                        ))
                    .Where(pair => pair.payment > 0).ToList();

				IPlayerInteraction mission = null;
                var totalKills = playerShip.shipsDestroyed
                    .Select(s => s.shipClass.codename)
                    .Intersect(rewardTable.Keys)
                    .Count();
                if(totalKills > 10) {
                }
                return new Dialog(
@"You are in the Militia Headquarters.",
                    [
                        new("Mission", Mission, enabled:mission != null),
                        new("Collect", Collect, enabled:groups.Any()),
                        new("Leave", Intro)
                        ]);
				IScene Mission (IScene prev) {
                    return null;
                }
				IScene Collect (IScene prev) {
					militiaRecordedKills.UnionWith(playerShip.shipsDestroyed);
                    playerShip.person.money += groups.Sum(pair => pair.payment);
                    return new Dialog(
@$"You collect payment for the following kills:

{string.Join("\n", groups.Select(pair => $"{pair.payment} for {pair.count}x {pair.name}"))}

Thank you for doing your part in maintaining
our collective security.",
                        [new("Continue", MilitiaHeadquarters)]);
                }
            }
        }
    }
    public IScene ConstellationVillage (IScene prev, PlayerShip playerShip, Station source) {
        return CheckConstellationArrest(prev, playerShip, source) ??
            Intro(prev);
		IScene Intro (IScene prev) {
            return new Dialog(
@"You are docked at a Constellation Village,
a residential station assembled out of 
shipping containers and various spare parts.",
                [
                    new("Meeting Hall", MeetingHall),
                    new("Undock")
                ]);
        }
		IScene MeetingHall (IScene prev) {
            var mission = mainInteractions.OfType<DestroyTarget>().FirstOrDefault(i => i.source == source);
            if (mission != null) {
                return mission.GetScene(prev, playerShip, source);
            }
            var target = source.world.entities.all.OfType<Station>().FirstOrDefault(s =>
                s.type.codename is "station_orion_warlords_camp"
                && (s.position - source.position).magnitude < 256
            );
            if (target == null) {
                return new Dialog(
@"The meeting hall is empty.",
                [
                    new("Leave", Intro)
                ]);
            }
            if (mainInteractions.OfType<DestroyTarget>().Any(i => i.targets.Contains(target))) {
                return new Dialog(
@"You aimlessly stand in the center of the empty Meeting Hall.
After 2 minutes, the station master approaches you.

""Hi, uh, we're currently dealing with a particularly annoying
Orion Warlords camp but I've been told that you're going to
destroy it for us. So, uh, thank you and good luck!""",
                [
                    new("Continue", Intro)
                ]);
            }
            return new Dialog(
@"You aimlessly stand in the center of the Meeting Hall.
After 10 minutes, the station master approaches you.

""Hi, uh, you seem to have a nice gunship. I'm currently
dealing with a nearby Orion Warlords outpost. They keep
sending us a lot of threats. We're not really worried
about being attacked so much as we just want them to
shut up. Even the health inspector is less asinine
than these idiots.""

""I'll pay you 400 to shut them up indefinitely.
What do you say?""",
            [
                new("Accept", Accept),
                new("Decline", Reject)
            ]);
			IScene Accept (IScene prev) {
                return new Dialog(
@"""Okay, thank you! Go destroy them and
then I'll see you back here.""",
                    [
                        new("Undock", Accepted)
                    ]);
            }
			Dialog Accepted (IScene prev) {
				DestroyTarget mission = null;
                mission = new DestroyTarget(playerShip, source, target) { inProgress = InProgress, debrief = Debrief };
				mainInteractions.Add(mission);
                return null;
				Dialog InProgress (IScene prev) {
                    return new(
@"""Hey, you're going to destroy that station, right?""",
                        [
                            new("Undock")
                        ]);
                }
				Dialog Debrief (IScene prev) {
                    return new(
@"""Thank you very much for destroying those warlords for us!
As promised, here's your 400""",
                        [
                            new("Undock", Debriefed)
                        ]);
                }
				Dialog Debriefed (IScene prev) {
                    playerShip.person.money += 400;
					mainInteractions.Remove(mission);
                    //completedInteractions.Add(mission);
                    return null;
                }
            }
			Dialog Reject (IScene prev) {
                return new(
@"""Oh man, what the hell is it with you people?
Okay, fine, I'll just find someone else to do it.""",
                    [
                        new("Undock")
                    ]);
            }
        }
    }

    public int contemplationCount;
    public IScene DaughtersOutpost (IScene prev, PlayerShip playerShip, Station source) {

        var status = (DaughtersOutpost)source.behavior;
        var univ = source.world.universe;
        void AddPower(string codename) {
            playerShip.powers.Add(new(univ.types.Lookup<PowerType>(codename)));
        }

        return Intro(prev);
		IScene Intro (IScene prev) {
            return new Dialog(
@"You are docked at an outpost of the
Daughters of the Orator.",
                [
                    new("Sanctum", Sanctum),
                    //new("Donation", null),
                    new("Undock", null)
                ]);
        }
		IScene Sanctum (IScene prev) {
            if (!status.sanctumReady && status.funds - 1000 is int i && i >= 0) {
                status.funds = i;
                status.sanctumReady = true;
            }
            return new Dialog(
@"You are in the sanctum of the Daughters
of the Orator. You hear a quiet hum all
around you. " + (status.sanctumReady ? "" :

@"The whole place is in shambles from a
previous contemplation ritual. Tools and
scaffolding lie scattered around the room in
anticipation of a badly delayed repair job.

One of the Daughters stands at the entrance.

""Sorry, the sanctum is closed for repairs.
We would appreciate any donation you could
make to help us cover the cost."""),
                [
                    new("Contemplate", Contemplate, enabled: status.sanctumReady),
                    new("Donate", Donate),
                    new("Leave", Intro)
                ]);
        }
		IScene Contemplate (IScene prev) {
			contemplationCount++;
            return new Dialog(
@"You attune yourself to the hum
that permeates throughout the sanctum.",
                [new("Continue", Result)]);
			IScene Result (IScene prev) {
                switch (contemplationCount) {
                    case 1:
                        AddPower("power_silence_orator");
                        return Info(prev,
@"The Orator grants you
the power of SILENCE.

""If the void is quiet,
then raise your voice.""");
                    case 3:
                        AddPower("power_recite_orator");
                        return Info(prev,
@"The Orator grants you
the power of RECITE.

""If you lose the sight of truth,
then RECITE the words against doom.""");
                    default:
                        return Shambles(prev);
                }

            }
			IScene Info (IScene prev, string desc) =>
                new Dialog(desc, [new("Continue", Shambles)]);
			IScene Shambles (IScene prev) {
                return new Dialog(
@"An unusual energy strikes and shakes
the Santum's micro-engraved plasteel-plated
walls as you contemplate.

Gems and jewels of communication, thrown off
of their stands on the walls by the tremors,
litter the floor.

When you are done contemplating, the Sanctum
is visibly damaged. The walls are scarred and
burnt from the bursts of channelled energy.

As you leave, abbey technicians
hurry in and begin checking the walls.",
                [
                    new("Continue", Done)
                ]);
				IScene Done (IScene prev) {
                    status.sanctumReady = false;
                    return Sanctum(prev);
                }
            }
        }
		IScene Donate (IScene prev) {
			Action a (Action b) => b;
            ListMenu<Item> screen = null;
            var dict = (new {
                item_prescience_book = a(() => {
                    screen.Replace(new Dialog(
@"""Thank you for your donation of-

Oh. This is garbage.

This book is actual garbage written by
someone trying to exploit The Orator's
influence for financial gain...

You didn't read it, did you?""",
                        [
                            new("Continue", Sanctum)
                        ]));
                }),
                item_gem_of_monologue = a(Regular)
            }).ToDict<Action>();
            void Regular() {
                status.funds += stdPrice[screen.list.currentItem.type];
                screen.Replace(new Dialog(
@"""Thank you for your donation of this
resonant artifact - may The Orator smile
upon you.""",
                    [
                        new("Continue", Sanctum)
                    ]));
            }

            return screen = new(prev, playerShip, $"{playerShip.name}: Cargo", playerShip.cargo.Where(i => dict.ContainsKey(i.type.codename)), (object i) => i.name, GetDesc, Choose, Escape);
            List<Tile[]> GetDesc (Item i) {
                List<Tile[]> result = [];
                var desc = i.type.desc.SplitLine(64);
                if (desc.Any()) {
                    result.AddRange(desc.Select(c => Tile.Arr(c, ABGR.White, ABGR.Black)));
                    result.Add([]);
                }
                result.Add(Tile.Arr("[Enter] Donate", ABGR.Yellow, ABGR.Black));
                return result;
            }
            void Choose(Item i) {
                playerShip.cargo.Remove(i);
                dict[i.type.codename]();
            }
            void Escape() {
                screen.Replace(prev);
            }
        }
    }
    public IScene OrionWarlordsCamp (IScene home, PlayerShip playerShip, Station source) {
		Dialog Intro (IScene prev) {
            return new(
source.damageSystem.GetHP() >= 50 ?
@"You are docked at an Orion Warlords Camp.
Enemy soldiers glare at you from the windows
of the station." :
@"You are docked at an Orion Warlords Camp.
Your ship identifies a distress signal
originating from this station.",
                [
                    new("Break In", BreakIn),
                    new("Undock")
                ]);
        }
		Dialog BreakIn (IScene prev) {
            if (source.damageSystem.GetHP() < 50) {
                if(source.behavior is Ob<Station.Destroyed> d)
                    source.onDestroyed -= d;
                source.Destroy(playerShip);
                var wreck = source.destroyed.wreck;
                return new(
@"You break down the entry gate with your primary weapon.
You make your way to the bridge and destroy the
black box, shutting off the distress signal.

You leave the station in ruins.",
                    [
                        new("Scavenge", Scavenge),
                        new("Undock")
                    ]);
				IScene Scavenge (IScene prev) => wreck.GetDockScene(home, playerShip);
            }
            return new(
@"The entry gate refuses to budge...",
                [
                    new("Continue", Intro)
                ]);
        }
        return Intro(home);
    }
}
public class DestroyTarget : IPlayerInteraction, Ob<AIShip.Destroyed>, Ob<Station.Destroyed> {
    public PlayerShip attacker;
    public Station source;
    public HashSet<ActiveObject> targets;
    public bool complete => targets.Count == 0;
    [JsonIgnore]
    public Func<IScene, IScene> inProgress, debrief;
    public DestroyTarget(PlayerShip attacker, Station source, params ActiveObject[] targets) {
        this.attacker = attacker;
        this.source = source;
        this.targets = new(targets);
        foreach (var t in targets) {
            switch (t) {
                case AIShip s:
                    s.onDestroyed += this;
                    break;
                case Station s:
                    s.onDestroyed += this;
                    break;
            }
        }
    }
    public void Observe(AIShip.Destroyed ev) {
        var (s, d, w) = ev;
        if (targets.Remove(s) && targets.Count == 0) {
            attacker.AddMessage(new Message("Mission complete!"));
            s.onDestroyed -= this;
        }
    }
    public void Observe(Station.Destroyed ev) {
        var (s, d, w) = ev;
        if (targets.Remove(s) && targets.Count == 0) {
            attacker.AddMessage(new Message("Mission complete!"));
            s.onDestroyed -= this;
        }
    }
    public IScene GetScene (IScene prev, PlayerShip playerShip, IDockable d) {
        if (d != source) {
            return null;
        }
        if (complete) {
            return debrief(prev);
        }
        return inProgress(prev);
    }
}

