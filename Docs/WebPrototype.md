# ⚔️ COUNTER / ARENA (Stage Exploration & Battle Transition Edition)
> **Combat Prototype / 03 with Mini-Dungeon Exploration & Battle Cuts**

---

## 🌐 ลิงก์เล่นเกม (Playable Links)

- **ลิงก์ถาวรบน GitHub Pages (24 ชม. ตลอดชีพ):** 👉 **[https://lnwzavbvb-tech.github.io/My-game/](https://lnwzavbvb-tech.github.io/My-game/)**
- **ลิงก์สำรอง (Cloudflare Live Tunnel):** 👉 **[https://liquid-parties-libs-firewire.trycloudflare.com](https://liquid-parties-libs-firewire.trycloudflare.com)**
- **GitHub Repository:** 👉 **[https://github.com/lnwzavbvb-tech/My-game](https://github.com/lnwzavbvb-tech/My-game)**

---

## 🗺️ ระบบใหม่: การเดินสำรวจด่าน & ตัดเข้าฉากต่อสู้ (Stage & Encounter System)

### 1. โหมดเดินสำรวจด่าน (Sector 07: Ruined Cyber Grid)
- **การควบคุมการเดิน**:
  - `W, A, S, D` หรือปุ่มลูกศร: เดินได้ 8 ทิศทางอย่างอิสระ
  - กด `Shift` หรือ `Space`: สปรินต์วิ่งเร็ว พร้อมเอฟเฟกต์ฝุ่นนีออนตามหลัง
- **พื้นที่ในด่าน**:
  - กำแพงและสิ่งกีดขวางแนว Cyber Sanctuary พร้อมระบบตรวจจับการชน (Collision)
  - กล่องไอเทมตามฉาก:
    - 🧪 **Nanite Medkit**: เดินชนเพื่อฟื้นฟูเลือด `+35 HP`
    - ⚡ **Adrenaline Core**: เดินชนเพื่อชาร์จเกจท่าไม้ตาย Ultimate เต็ม 100%!

### 2. ศัตรูลาดตระเวน 3 รูปแบบในด่าน
1. **Cyber Sentinel (Drone ลาดตระเวน)**:
   - บินตรวจการณ์ไป-มาบริเวณทางเดินด้านขวา
   - เลือด 140 HP | ท่าโจมตี Rapid Jab String
2. **Heavy Enforcer (หุ่นเกราะหนา)**:
   - เดินเฝ้าทางเชื่อมห้องโถงกลาง
   - เลือด 220 HP | ท่าโจมตี Crusher Barrage
3. **Iron Warden [Sector Boss]**:
   - ยืนเฝ้าอยู่หน้าประตูใหญ่ด้านบนสุดของด่าน มีออร่าเรืองแสงและสัญลักษณ์วงเวทย์อันตราย
   - เลือด 320 HP | ท่าโจมตีคอมโบต่อเนื่อง 3 ฮิต (Iron Tempest & Vortex Riposte)

### 3. ระบบตัดเข้าฉากต่อสู้ (Cinematic Battle Cut)
- เมื่อผู้เล่นเดินเข้าใกล้ศัตรูในระยะประชิด:
  - จอจะเกิดรอยตัดเลเซอร์นีออน (Slash Cutlines) ขวางหน้าจอ
  - เสียงเตือนภัยและเสียงกระแทกทรงพลัง `ENCOUNTER!`
  - ตัดเข้าสู่ **Counter Arena Battle** สู้กับศัตรูตัวนั้นทันที!
- เมื่อเอาชนะศัตรูได้:
  - ผู้เล่นจะได้รับคะแนนและฟื้นฟูเลือด `+25 HP`
  - จอจะตัดกลับมาที่หน้าด่านสำรวจเดิม โดยศัตรูที่ถูกกำจัดจะหายไป
  - เมื่อกำจัดครบทั้ง 3 ตัว จะขึ้นหน้าต่าง **SECTOR 07 LIBERATED!** ชนะด่านอย่างสมบูรณ์!
