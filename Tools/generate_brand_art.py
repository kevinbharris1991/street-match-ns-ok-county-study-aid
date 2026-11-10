#!/usr/bin/env python3
"""Generate the game's icon and splash artwork using only the Python standard library."""

import os
import struct
import zlib

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
FONT = {
    "A": ("01110", "10001", "10001", "11111", "10001", "10001", "10001"),
    "B": ("11110", "10001", "10001", "11110", "10001", "10001", "11110"),
    "C": ("01111", "10000", "10000", "10000", "10000", "10000", "01111"),
    "D": ("11110", "10001", "10001", "10001", "10001", "10001", "11110"),
    "E": ("11111", "10000", "10000", "11110", "10000", "10000", "11111"),
    "F": ("11111", "10000", "10000", "11110", "10000", "10000", "10000"),
    "G": ("01111", "10000", "10000", "10111", "10001", "10001", "01111"),
    "H": ("10001", "10001", "10001", "11111", "10001", "10001", "10001"),
    "I": ("11111", "00100", "00100", "00100", "00100", "00100", "11111"),
    "J": ("00111", "00010", "00010", "00010", "10010", "10010", "01100"),
    "K": ("10001", "10010", "10100", "11000", "10100", "10010", "10001"),
    "L": ("10000", "10000", "10000", "10000", "10000", "10000", "11111"),
    "M": ("10001", "11011", "10101", "10101", "10001", "10001", "10001"),
    "N": ("10001", "11001", "10101", "10011", "10001", "10001", "10001"),
    "O": ("01110", "10001", "10001", "10001", "10001", "10001", "01110"),
    "P": ("11110", "10001", "10001", "11110", "10000", "10000", "10000"),
    "Q": ("01110", "10001", "10001", "10001", "10101", "10010", "01101"),
    "R": ("11110", "10001", "10001", "11110", "10100", "10010", "10001"),
    "S": ("01111", "10000", "10000", "01110", "00001", "00001", "11110"),
    "T": ("11111", "00100", "00100", "00100", "00100", "00100", "00100"),
    "U": ("10001", "10001", "10001", "10001", "10001", "10001", "01110"),
    "V": ("10001", "10001", "10001", "10001", "10001", "01010", "00100"),
    "W": ("10001", "10001", "10001", "10101", "10101", "10101", "01010"),
    "X": ("10001", "10001", "01010", "00100", "01010", "10001", "10001"),
    "Y": ("10001", "10001", "01010", "00100", "00100", "00100", "00100"),
    "Z": ("11111", "00001", "00010", "00100", "01000", "10000", "11111"),
    "0": ("01110", "10001", "10011", "10101", "11001", "10001", "01110"),
    "1": ("00100", "01100", "00100", "00100", "00100", "00100", "01110"),
    "2": ("01110", "10001", "00001", "00010", "00100", "01000", "11111"),
    "3": ("11110", "00001", "00001", "01110", "00001", "00001", "11110"),
    "4": ("00010", "00110", "01010", "10010", "11111", "00010", "00010"),
    "5": ("11111", "10000", "10000", "11110", "00001", "00001", "11110"),
    "6": ("01110", "10000", "10000", "11110", "10001", "10001", "01110"),
    "7": ("11111", "00001", "00010", "00100", "01000", "01000", "01000"),
    "8": ("01110", "10001", "10001", "01110", "10001", "10001", "01110"),
    "9": ("01110", "10001", "10001", "01111", "00001", "00001", "01110"),
    "&": ("01100", "10010", "10100", "01000", "10101", "10010", "01101"),
    "-": ("00000", "00000", "00000", "11111", "00000", "00000", "00000"),
    ".": ("00000", "00000", "00000", "00000", "00000", "01100", "01100"),
    "/": ("00001", "00010", "00010", "00100", "01000", "01000", "10000"),
    ":": ("00000", "01100", "01100", "00000", "01100", "01100", "00000"),
}

INK = (18, 24, 43)
PANEL = (29, 38, 61)
CARD = (40, 52, 79)
MINT = (111, 235, 193)
LAVENDER = (190, 175, 255)
WHITE = (242, 245, 255)
MUTED = (167, 180, 207)


class Canvas:
    def __init__(self, width, height, color=INK):
        self.width = width
        self.height = height
        self.pixels = bytearray(color * width * height)

    def pixel(self, x, y, color):
        if 0 <= x < self.width and 0 <= y < self.height:
            i = (y * self.width + x) * 3
            self.pixels[i:i + 3] = bytes(color)

    def rect(self, x, y, width, height, color):
        x0, y0 = max(0, int(x)), max(0, int(y))
        x1, y1 = min(self.width, int(x + width)), min(self.height, int(y + height))
        row = bytes(color) * max(0, x1 - x0)
        for py in range(y0, y1):
            i = (py * self.width + x0) * 3
            self.pixels[i:i + len(row)] = row

    def rounded(self, x, y, width, height, radius, color):
        for py in range(max(0, y), min(self.height, y + height)):
            for px in range(max(0, x), min(self.width, x + width)):
                cx = max(x + radius, min(px, x + width - radius - 1))
                cy = max(y + radius, min(py, y + height - radius - 1))
                if (px - cx) ** 2 + (py - cy) ** 2 <= radius ** 2:
                    self.pixel(px, py, color)

    def circle(self, cx, cy, radius, color):
        for y in range(max(0, cy - radius), min(self.height, cy + radius + 1)):
            for x in range(max(0, cx - radius), min(self.width, cx + radius + 1)):
                if (x - cx) ** 2 + (y - cy) ** 2 <= radius ** 2:
                    self.pixel(x, y, color)

    def line(self, x0, y0, x1, y1, width, color):
        dx, dy = x1 - x0, y1 - y0
        steps = max(abs(dx), abs(dy), 1)
        for i in range(steps + 1):
            x = round(x0 + dx * i / steps)
            y = round(y0 + dy * i / steps)
            self.circle(x, y, width // 2, color)

    def text(self, value, x, y, scale, color, centered=False):
        value = value.upper()
        advance = 6 * scale
        if centered:
            x -= (len(value) * advance - scale) // 2
        for index, character in enumerate(value):
            glyph = FONT.get(character)
            if not glyph:
                continue
            gx = x + index * advance
            for row, bits in enumerate(glyph):
                for column, bit in enumerate(bits):
                    if bit == "1":
                        self.rect(gx + column * scale, y + row * scale,
                                  scale, scale, color)

    def png(self, path, scale=1):
        width, height = self.width * scale, self.height * scale
        raw = bytearray()
        row_length = self.width * 3
        for y in range(self.height):
            row = self.pixels[y * row_length:(y + 1) * row_length]
            row = b"".join(row[i:i + 3] * scale for i in range(0, len(row), 3))
            for _ in range(scale):
                raw.append(0)
                raw.extend(row)

        def chunk(kind, data):
            payload = kind + data
            return struct.pack(">I", len(data)) + payload + struct.pack(">I", zlib.crc32(payload) & 0xFFFFFFFF)

        image = b"\x89PNG\r\n\x1a\n"
        image += chunk(b"IHDR", struct.pack(">2I5B", width, height, 8, 2, 0, 0, 0))
        image += chunk(b"IDAT", zlib.compress(bytes(raw), 9))
        image += chunk(b"IEND", b"")
        os.makedirs(os.path.dirname(path), exist_ok=True)
        with open(path, "wb") as output:
            output.write(image)


def icon():
    image = Canvas(256, 256)
    image.rounded(15, 15, 226, 226, 38, PANEL)
    image.rounded(36, 36, 184, 184, 28, INK)
    image.line(128, 57, 128, 199, 4, CARD)
    for y in range(74, 190, 28):
        image.rect(125, y, 6, 12, MUTED)
    image.rounded(43, 82, 82, 78, 13, MINT)
    image.rounded(131, 96, 82, 78, 13, LAVENDER)
    image.text("N", 84, 105, 10, INK)
    image.text("S", 173, 119, 10, INK)
    image.circle(128, 180, 19, WHITE)
    image.text("=", 113, 174, 5, INK)
    image.png(os.path.join(ROOT, "Assets", "Art", "StreetMatchIcon.png"), 2)


def splash():
    image = Canvas(960, 540)
    for y in range(image.height):
        blend = y / image.height
        color = tuple(int(INK[i] * (1 - blend) + PANEL[i] * blend * .55) for i in range(3))
        image.rect(0, y, image.width, 1, color)

    for x in range(40, 960, 80):
        image.line(x, 0, x, 540, 1, (24, 32, 54))
    for y in range(30, 540, 80):
        image.line(0, y, 960, y, 1, (24, 32, 54))

    image.rounded(91, 48, 58, 58, 12, MINT)
    image.rounded(811, 48, 58, 58, 12, LAVENDER)
    image.text("N", 112, 62, 5, INK)
    image.text("S", 832, 62, 5, INK)
    image.text("STREET MATCH", 480, 73, 8, WHITE, centered=True)
    image.text("N & S  /  OK COUNTY STUDY AID", 480, 149, 3, MUTED, centered=True)

    image.rounded(62, 224, 324, 122, 18, (12, 18, 34))
    image.rounded(574, 224, 324, 122, 18, (12, 18, 34))
    image.rounded(68, 218, 324, 122, 18, MINT)
    image.rounded(568, 218, 324, 122, 18, LAVENDER)
    image.text("NORTH STREET", 230, 240, 2, INK, centered=True)
    image.text("SHARTEL AVE", 230, 278, 4, INK, centered=True)
    image.text("BLOCK NUMBER", 730, 240, 2, INK, centered=True)
    image.text("2400", 730, 268, 8, INK, centered=True)
    image.line(420, 279, 540, 279, 4, MUTED)
    image.circle(480, 279, 27, WHITE)
    image.text("=", 465, 268, 5, INK)
    image.text("MATCH STREETS. BUILD CONFIDENCE.", 480, 346, 4, WHITE, centered=True)
    image.text("LEARN THE NORTH AND SOUTH ROUTES OF OK COUNTY", 480, 384, 2, MUTED, centered=True)
    image.text("UNOFFICIAL STUDY AID - NOT CERTIFIED EMS TRAINING", 480, 426, 2, MUTED, centered=True)
    image.text("OR AN OPERATIONAL NAVIGATIONAL TOOL", 480, 450, 2, MUTED, centered=True)
    image.png(os.path.join(ROOT, "Assets", "Resources", "StreetMatchSplash.png"), 2)


if __name__ == "__main__":
    icon()
    splash()
