"""Sintetiza todos os efeitos sonoros e músicas do jogo (originais, gerados por código)."""
import os
import sys
import wave
import numpy as np
from scipy.signal import lfilter, butter

SR = 22050
OUT = sys.argv[1] if len(sys.argv) > 1 else "sons"
rng = np.random.default_rng(7)


def t(d):
    return np.arange(int(SR * d)) / SR


def env(n, a=0.005, d=0.1, curve=4.0):
    x = np.arange(n) / SR
    e = np.minimum(1.0, x / max(a, 1e-4)) * np.exp(-curve * x / max(d, 1e-4))
    return e


def noise(d):
    return rng.uniform(-1, 1, int(SR * d))


def lp(x, f):
    b, a = butter(2, f / (SR / 2), "low")
    return lfilter(b, a, x)


def hp(x, f):
    b, a = butter(2, f / (SR / 2), "high")
    return lfilter(b, a, x)


def bp(x, lo, hi):
    b, a = butter(2, [lo / (SR / 2), hi / (SR / 2)], "band")
    return lfilter(b, a, x)


def sine(f, d, phase=0):
    tt = t(d)
    if callable(f):
        return np.sin(2 * np.pi * np.cumsum(f(tt)) / SR)
    return np.sin(2 * np.pi * f * tt + phase)


def square(f, d, duty=0.5):
    tt = t(d)
    return np.where((tt * f) % 1.0 < duty, 1.0, -1.0)


def tri(f, d):
    tt = t(d)
    return 2 * np.abs(2 * ((tt * f) % 1.0) - 1) - 1


def mix(*parts):
    n = max(len(p) for p in parts)
    out = np.zeros(n)
    for p in parts:
        out[:len(p)] += p
    return out


def pad(x, d):
    n = int(SR * d)
    return np.concatenate([x, np.zeros(max(0, n - len(x)))])[:max(n, len(x))]


def save(name, x, vol=0.8):
    x = np.asarray(x, dtype=np.float64)
    peak = np.max(np.abs(x)) + 1e-9
    x = x / peak * vol
    # fade de 5ms nas pontas para não estalar
    f = min(len(x) // 4, int(SR * 0.005))
    if f > 0:
        x[:f] *= np.linspace(0, 1, f)
        x[-f:] *= np.linspace(1, 0, f)
    data = (x * 32767).astype(np.int16)
    os.makedirs(OUT, exist_ok=True)
    with wave.open(f"{OUT}/{name}.wav", "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(data.tobytes())


def note(n):
    """nome de nota -> frequência (ex.: 'A4', 'C#5')"""
    names = {"C": -9, "C#": -8, "D": -7, "D#": -6, "E": -5, "F": -4, "F#": -3, "G": -2, "G#": -1, "A": 0, "A#": 1, "B": 2}
    pitch, octave = n[:-1], int(n[-1])
    return 440.0 * 2 ** ((names[pitch] + 12 * (octave - 4)) / 12)


# ============================================================ EFEITOS

def sfx():
    # espada: assobio + tilintar metálico
    sw = hp(noise(0.16), 2500) * env(int(SR * 0.16), 0.02, 0.08)
    ping = (sine(1900, 0.25) + 0.6 * sine(2850, 0.25) + 0.3 * sine(4100, 0.25)) * env(int(SR * 0.25), 0.002, 0.08)
    save("espada", mix(sw, np.concatenate([np.zeros(int(SR * 0.06)), ping * 0.5])), 0.6)

    # golpe pesado (corpo a corpo grande)
    thud = sine(lambda x: 140 * np.exp(-x * 8) + 45, 0.3) * env(int(SR * 0.3), 0.002, 0.12)
    save("golpe", mix(thud, lp(noise(0.12), 1500) * env(int(SR * 0.12), 0.001, 0.04) * 0.6), 0.75)

    # flecha
    wh = bp(noise(0.25), 1500, 5000) * env(int(SR * 0.25), 0.03, 0.09)
    save("flecha", wh * np.linspace(1, 0.3, len(wh)), 0.5)

    # tiro (atiradora)
    shot = mix(hp(noise(0.18), 800) * env(int(SR * 0.18), 0.001, 0.04),
               sine(lambda x: 300 * np.exp(-x * 20) + 80, 0.18) * env(int(SR * 0.18), 0.001, 0.05))
    save("tiro", shot, 0.6)

    # canhão do rei
    boom = mix(sine(lambda x: 90 * np.exp(-x * 6) + 35, 0.6) * env(int(SR * 0.6), 0.002, 0.2),
               lp(noise(0.5), 900) * env(int(SR * 0.5), 0.001, 0.12) * 0.8)
    save("canhao", boom, 0.8)

    # magia (zap subindo)
    zap = sine(lambda x: 500 + 1800 * x / 0.35 + 60 * np.sin(2 * np.pi * 30 * x), 0.35) * env(int(SR * 0.35), 0.01, 0.15)
    save("magia", zap + 0.2 * hp(noise(0.35), 4000) * env(int(SR * 0.35), 0.01, 0.1), 0.45)

    # gelo (cristal)
    ice = sum(sine(f, 0.5) * env(int(SR * 0.5), 0.002, 0.15 + i * 0.03) for i, f in enumerate([1760, 2349, 3136, 3520]))
    save("gelo", ice, 0.4)

    # explosão (bola de fogo, bomba, meteoro)
    ex = mix(lp(noise(1.0), 1200) * env(int(SR * 1.0), 0.003, 0.3, 3.5),
             sine(lambda x: 70 * np.exp(-x * 3) + 30, 1.0) * env(int(SR * 1.0), 0.003, 0.35))
    save("explosao", ex, 0.85)
    small = mix(lp(noise(0.45), 1800) * env(int(SR * 0.45), 0.002, 0.15),
                sine(lambda x: 110 * np.exp(-x * 6) + 40, 0.45) * env(int(SR * 0.45), 0.002, 0.15))
    save("bomba", small, 0.7)

    # raio: estalos + estrondo
    crack = np.zeros(int(SR * 0.9))
    for _ in range(40):
        p = rng.integers(0, int(SR * 0.25))
        crack[p:p + 200] += rng.uniform(-1, 1, 200)[: len(crack[p:p + 200])]
    crack = hp(crack, 1500) + lp(noise(0.9), 300) * env(int(SR * 0.9), 0.05, 0.35) * 1.5
    save("raio", crack * env(len(crack), 0.001, 0.4, 2.5), 0.8)

    # tropa caindo na arena
    land = mix(sine(lambda x: 160 * np.exp(-x * 14) + 55, 0.22) * env(int(SR * 0.22), 0.001, 0.07),
               lp(noise(0.15), 2500) * env(int(SR * 0.15), 0.001, 0.03) * 0.5)
    save("queda", land, 0.65)

    # torre desmoronando
    rumble = lp(noise(1.8), 400) * env(int(SR * 1.8), 0.02, 0.7, 2.0)
    debris = np.zeros(int(SR * 1.8))
    for _ in range(25):
        p = rng.integers(int(SR * 0.1), int(SR * 1.4))
        clk = bp(noise(0.06), 600, 3000) * env(int(SR * 0.06), 0.001, 0.02)
        debris[p:p + len(clk)] += clk[: len(debris[p:p + len(clk)])]
    save("torre", mix(rumble * 1.3, debris * 0.6, sine(lambda x: 60 * np.exp(-x) + 30, 1.8) * env(int(SR * 1.8), 0.01, 0.5)), 0.9)

    # morte de tropa (puf)
    poof = bp(noise(0.3), 400, 2500) * env(int(SR * 0.3), 0.01, 0.1)
    save("puf", poof, 0.45)

    # interface
    save("botao", mix(sine(1200, 0.06) * env(int(SR * 0.06), 0.001, 0.02), square(600, 0.04) * 0.2 * env(int(SR * 0.04), 0.001, 0.01)), 0.4)
    save("erro", mix(square(220, 0.18, 0.3) * env(int(SR * 0.18), 0.002, 0.08), square(180, 0.18, 0.3) * 0.7), 0.35)
    coin = np.concatenate([sine(988, 0.07) * env(int(SR * 0.07), 0.001, 0.05), sine(1319, 0.3) * env(int(SR * 0.3), 0.001, 0.12)])
    save("moeda", coin, 0.45)
    save("carta", sum(sine(note(n), 0.6) * env(int(SR * 0.6), 0.002, 0.2) for n in ["E6", "B6", "E7"]), 0.4)

    # baú abrindo: rangido + brilho
    creak = sine(lambda x: 180 + 60 * np.sin(2 * np.pi * 7 * x), 0.35) * env(int(SR * 0.35), 0.05, 0.25) * 0.5
    sparkle = np.zeros(int(SR * 0.8))
    for i, n in enumerate(["C6", "E6", "G6", "C7", "E7"]):
        s = sine(note(n), 0.4) * env(int(SR * 0.4), 0.002, 0.12)
        p = int(SR * (0.3 + i * 0.07))
        sparkle[p:p + len(s)] += s[: len(sparkle[p:p + len(s)])]
    save("bau", mix(creak, sparkle), 0.55)
    shake = bp(noise(0.12), 200, 1200) * env(int(SR * 0.12), 0.005, 0.05)
    save("bau_treme", shake, 0.5)

    # evoluir carta: arpejo subindo
    up = np.concatenate([square(note(n), 0.09, 0.25) * env(int(SR * 0.09), 0.002, 0.07) for n in ["C5", "E5", "G5", "C6", "E6", "G6"]])
    save("evoluir", mix(up, np.concatenate([np.zeros(len(up) - 100), sine(note("C7"), 0.5) * env(int(SR * 0.5), 0.002, 0.2)])), 0.4)

    # contagem e início
    save("bip", sine(880, 0.15) * env(int(SR * 0.15), 0.002, 0.08), 0.45)
    go = sum(square(note(n), 0.6, 0.3) * env(int(SR * 0.6), 0.002, 0.25) for n in ["C5", "E5", "G5", "C6"])
    save("batalha", go, 0.45)

    # coroa
    save("coroa", sum(sine(note(n), 0.9) * env(int(SR * 0.9), 0.002, 0.35) for n in ["G5", "B5", "D6", "G6"]), 0.5)

    # aviso de último minuto
    alarm = np.concatenate([square(note(n), 0.14, 0.4) * env(int(SR * 0.14), 0.002, 0.1) for n in ["A5", "E5", "A5", "E5"]])
    save("alerta", alarm, 0.35)

    # vitória / derrota
    fan = []
    for n, d in [("C5", 0.15), ("E5", 0.15), ("G5", 0.15), ("C6", 0.45), ("A5", 0.15), ("C6", 0.7)]:
        fan.append((square(note(n), d, 0.25) * 0.6 + tri(note(n) / 2, d)) * env(int(SR * d), 0.005, d * 0.8, 1.5))
    save("vitoria", np.concatenate(fan), 0.55)
    sad = []
    for n, d in [("G4", 0.3), ("F#4", 0.3), ("F4", 0.3), ("E4", 0.9)]:
        sad.append((tri(note(n), d) + 0.3 * square(note(n), d, 0.15)) * env(int(SR * d), 0.01, d * 0.7, 1.5))
    save("derrota", np.concatenate(sad), 0.5)


# ============================================================ MÚSICAS

def drum_kick():
    return sine(lambda x: 120 * np.exp(-x * 25) + 45, 0.25) * env(int(SR * 0.25), 0.001, 0.12)


def drum_snare():
    return mix(bp(noise(0.18), 1500, 6000) * env(int(SR * 0.18), 0.001, 0.06),
               sine(190, 0.1) * env(int(SR * 0.1), 0.001, 0.04) * 0.5)


def drum_hat():
    return hp(noise(0.05), 7000) * env(int(SR * 0.05), 0.001, 0.015)


def place(buf, sig, at, gain=1.0):
    p = int(at * SR)
    if p >= len(buf):
        return
    n = min(len(sig), len(buf) - p)
    buf[p:p + n] += sig[:n] * gain


def song(bpm, chords, melody, bass_style, lead_wave, drums, bars_per_chord=1, swing=0.0, lead_gain=0.32):
    beat = 60.0 / bpm
    bar = beat * 4
    total = bar * len(chords) * bars_per_chord
    buf = np.zeros(int(total * SR) + 1)
    # acordes (pad suave)
    for i, ch in enumerate(chords):
        start = i * bar * bars_per_chord
        d = bar * bars_per_chord
        pad_sig = sum(lp(square(note(n), d, 0.5), 1400) for n in ch) / len(ch)
        pad_sig *= np.minimum(1, t(d) / 0.05) * np.minimum(1, (d - t(d)) / 0.08)
        place(buf, pad_sig, start, 0.10)
        # baixo
        root = note(ch[0][:-1] + str(int(ch[0][-1]) - 1))
        steps = 8 if bass_style == "oitavas" else 4
        for k in range(steps * bars_per_chord):
            f = root * (2 if (bass_style == "oitavas" and k % 2) else 1)
            dd = bar / steps * 0.9
            place(buf, tri(f, dd) * env(int(SR * dd), 0.005, dd, 1.0), start + k * bar / steps, 0.35)
    # melodia
    pos = 0.0
    for n, beats in melody:
        d = beats * beat
        if n != "-":
            f = note(n)
            sig = (square(f, d, 0.25) if lead_wave == "pulso" else tri(f, d) * 0.8 + square(f, d, 0.12) * 0.2)
            vib = 1.0
            sig = lp(sig, 3500) * env(int(SR * d), 0.01, d * 1.2, 1.2)
            place(buf, sig, pos, lead_gain)
        pos += d
    # bateria
    nb = int(total / beat)
    for b in range(nb):
        at = b * beat
        if drums == "leve":
            if b % 4 == 0:
                place(buf, drum_kick(), at, 0.5)
            if b % 4 == 2:
                place(buf, drum_snare(), at, 0.25)
            place(buf, drum_hat(), at + beat / 2, 0.12)
        else:
            if b % 2 == 0:
                place(buf, drum_kick(), at, 0.6)
            if b % 4 in (1, 3):
                place(buf, drum_snare(), at, 0.35)
            place(buf, drum_hat(), at, 0.12)
            place(buf, drum_hat(), at + beat / 2, 0.16)
            if drums == "intensa":
                place(buf, drum_kick(), at + beat * 0.75, 0.35)
    return buf[: int(total * SR)]


def musics():
    # Lobby: alegre, Dó maior
    chords = [["C4", "E4", "G4"], ["A3", "C4", "E4"], ["F3", "A3", "C4"], ["G3", "B3", "D4"]] * 2
    mel = [("E5", 1), ("G5", 1), ("C6", 1.5), ("B5", 0.5), ("A5", 1), ("G5", 1), ("E5", 1), ("C5", 1),
           ("F5", 1), ("A5", 1), ("G5", 1.5), ("F5", 0.5), ("E5", 2), ("D5", 2),
           ("E5", 1), ("G5", 1), ("C6", 1), ("D6", 1), ("E6", 1.5), ("D6", 0.5), ("C6", 1), ("A5", 1),
           ("F5", 1), ("A5", 1), ("C6", 1), ("B5", 1), ("C6", 3), ("-", 1)]
    save("musica_lobby", song(112, chords, mel, "simples", "suave", "leve", lead_gain=0.28), 0.6)

    # Batalha: Lá menor, mais pegada
    chords = [["A3", "C4", "E4"], ["F3", "A3", "C4"], ["G3", "B3", "D4"], ["E3", "G#3", "B3"]] * 2
    mel = [("A4", 0.5), ("C5", 0.5), ("E5", 1), ("D5", 0.5), ("C5", 0.5), ("B4", 1), ("C5", 1), ("A4", 1),
           ("F5", 0.5), ("E5", 0.5), ("D5", 1), ("C5", 1), ("D5", 0.5), ("E5", 0.5), ("G5", 1), ("E5", 1), ("D5", 1),
           ("B4", 1), ("G#4", 1), ("B4", 1), ("E5", 1),
           ("A5", 0.5), ("G5", 0.5), ("E5", 1), ("C5", 1), ("E5", 1), ("F5", 1), ("E5", 0.5), ("D5", 0.5), ("C5", 1), ("A4", 1),
           ("D5", 1), ("C5", 1), ("B4", 1), ("D5", 1), ("E5", 2), ("B4", 1)]
    save("musica_batalha", song(132, chords, mel, "oitavas", "pulso", "normal", lead_gain=0.25), 0.6)

    # Último minuto: a mesma ideia, mais rápida e intensa
    save("musica_final", song(158, chords, mel, "oitavas", "pulso", "intensa", lead_gain=0.27), 0.65)


if __name__ == "__main__":
    sfx()
    musics()
    print("ok", sorted(os.listdir(OUT)))
