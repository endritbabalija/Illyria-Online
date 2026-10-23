// Match op codes. Every payload is UTF-8 JSON. server/PROTOCOL.md documents each message.

/** Client → server. */
var Op = {
    /** {x, y, z, yaw}: where the player is now (send ~10 times a second while moving). */
    Move: 1,
    /** {id} lock a monster (auto-attack starts in range) · {id: null} clear · {nearest: true} the Attack button. */
    Target: 2,
    /** {form, target?, dir?: [x, z]}: use a skill form; queued if one is still playing. */
    Cast: 3,
    /** {ledge?, dir?: [x, z]}: Drangue Leap onto a named ledge, or a ground dash without one. */
    Leap: 4,
    /** {a, rid?, …}: an item or skill-book action; answered with Result. */
    Action: 5,

    /** Server → joiner: the whole zone and your own character. */
    Welcome: 100,
    /** Server → all, every tick something changed: {t, p: [players], m: [monsters]} (changed entities only). */
    Snapshot: 101,
    /** Server → all: {t, e: [events]} (hits, casts, deaths, spawns, joins, leaves, leaps, gear). */
    Events: 102,
    /** Server → one player: your character's private state (bag, gear, stats, skills, gold). */
    Self: 103,
    /** Server → one player: your loot on the ground {add: [drops], remove: [{id, why}]}. */
    Drops: 104,
    /** Server → one player: the answer to an Action, Cast, Leap or Target {rid?, a, ok, error?, text?}. */
    Result: 105,
    /** Server → one player: a rejected move; you are at {x, y, z} {why}. */
    Correction: 106,
    /** Server → one player: a notice to show {code, text}. */
    Notice: 107,
};

/** Reasons a request was refused, with the text the client shows (matching the offline wording). */
var ErrorText: { [code: string]: string } = {
    dead: "You can't do that now",
    disabled: "You can't act right now",
    busy: "You can't do that now",
    safeZone: "You can't fight here",
    badRequest: 'Something went wrong',
    noTarget: 'No target nearby',
    outOfRange: 'Too far away',
    wrongWeapon: "That form needs another weapon",
    notLearned: "You haven't learned that book",
    locked: 'That form is still locked',
    cooldown: "That isn't ready",
    noSpirit: 'Not enough Spirit',
    maxLevel: 'That book is at its highest level',
    notEnoughPoints: 'Not enough skill points',
    notALedge: "You can't land there",
    tooFar: 'That ledge is too far',
    tooHigh: 'That ledge is too high',
    tooLow: 'That ledge is too low',
    notInAim: "That ledge isn't ahead of you",
    alreadyThere: "You're already there",
    notFound: "That item isn't in your bag",
    notGear: "That can't be worn",
    wrongCharacter: "Your character can't use that",
    levelTooLow: 'Your level is too low',
    bagFull: 'Your bag is full',
    weaponRequired: 'You always hold a weapon; equip another instead',
    noOtherWeapon: 'No other weapon in your bag',
    notSold: "That isn't sold here",
    badCount: 'Pick how many',
    notEnoughGold: 'Not enough gold',
    equipped: 'Take it off before selling it',
    goldLimit: "You can't carry more gold",
    noShop: 'Nobody here to trade with',
    noneLeft: 'None left',
    notUsable: 'Nothing to use there',
    alreadyFull: 'Already full',
    badSlot: "That can't go there",
};
