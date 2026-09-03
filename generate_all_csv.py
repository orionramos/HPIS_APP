import os
import csv
import re
import json
from tinytag import TinyTag

audios_dir = r"D:\GitHub\HPIS_APP\Assets\StreamingAssets\Audios"
videos_dir = r"D:\GitHub\HPIS_APP\Assets\StreamingAssets\Videos"
models_dir = r"D:\GitHub\HPIS_APP\Assets\Models"
json_db = r"D:\GitHub\HPIS_APP\Assets\StreamingAssets\feedback_database.json"
output_csv = r"D:\GitHub\HPIS_APP\HPIS_Clean_Multimodal_Data_Final.csv"

pattern_audio = re.compile(r"example_audio_(\d+)_(\d+)_(\d+)\.mp3")
pattern_image = re.compile(r"example_image_(\d+)_(\d+)_(\d+)\.mp4")
pattern_3d = re.compile(r"VideoAlpha_(\d+)_(\d+)_(\d+)\.mp4")

rows = []
file_durations = {}
audio_count = 0
image_count = 0
td_count = 0
comb_count = 0

print("Scanning Audios directory...")
for filename in os.listdir(audios_dir):
    match = pattern_audio.match(filename)
    if match:
        actividad, estrategia, paso = match.groups()
        filepath = os.path.join(audios_dir, filename)
        try:
            tag = TinyTag.get(filepath)
            duration = round(tag.duration, 3)
            rows.append({
                'Instruction_Time': duration,
                'Tipo': 'audio',
                'actividad': actividad,
                'HRI_strategy': estrategia,
                'paso_actividad': paso
            })
            key = filename.replace('.mp3', '')
            file_durations[key] = duration
            audio_count += 1
        except Exception:
            pass

print("Scanning Videos directory for example_image and VideoAlpha...")
for filename in os.listdir(videos_dir):
    match_image = pattern_image.match(filename)
    match_3d = pattern_3d.match(filename)
    
    if match_image:
        actividad, estrategia, paso = match_image.groups()
        filepath = os.path.join(videos_dir, filename)
        try:
            tag = TinyTag.get(filepath)
            duration = round(tag.duration, 3)
            rows.append({
                'Instruction_Time': duration,
                'Tipo': 'image',
                'actividad': actividad,
                'HRI_strategy': estrategia,
                'paso_actividad': paso
            })
            key = filename.replace('.mp4', '')
            file_durations[key] = duration
            image_count += 1
        except Exception:
            pass
    elif match_3d:
        actividad, estrategia, paso = match_3d.groups()
        filepath = os.path.join(videos_dir, filename)
        try:
            tag = TinyTag.get(filepath)
            duration = round(tag.duration, 3)
            rows.append({
                'Instruction_Time': duration,
                'Tipo': '3d',
                'actividad': actividad,
                'HRI_strategy': estrategia,
                'paso_actividad': paso
            })
            key = filename.replace('.mp4', '')
            file_durations[key] = duration
            td_count += 1
        except Exception:
            pass

print("Scanning Models directory for FBX meta animations...")
for filename in os.listdir(models_dir):
    if filename.endswith(".fbx.meta"):
        filepath = os.path.join(models_dir, filename)
        with open(filepath, 'r', encoding='utf-8') as f:
            content = f.read()
            matches = re.finditer(r"name:\s*Act(\d+)_(\d+).*?firstFrame:\s*(\d+).*?lastFrame:\s*(\d+)", content, re.DOTALL)
            for m in matches:
                actividad = m.group(1)
                estrategia = "6"
                paso = m.group(2)
                first_frame = int(m.group(3))
                last_frame = int(m.group(4))
                duration = round((last_frame - first_frame) / 24.0, 3)
                rows.append({
                    'Instruction_Time': duration,
                    'Tipo': '3d',
                    'actividad': actividad,
                    'HRI_strategy': estrategia,
                    'paso_actividad': paso
                })
                key = f"Act{actividad}_{paso}"
                file_durations[key] = duration
                td_count += 1

print("Parsing feedback_database.json for combinations...")
if os.path.exists(json_db):
    with open(json_db, 'r', encoding='utf-8') as f:
        db = json.load(f)
        for activity in db.get('activities', []):
            act_id = activity.get('id')
            for strategy in activity.get('strategies', []):
                strat_id = strategy.get('id')
                if strat_id in [7, 8, 9]:
                    for step in strategy.get('steps', []):
                        paso_id = step.get('id')
                        content_val = step.get('contentValue', '')
                        if not content_val: continue
                        
                        parts = content_val.split('-')
                        dur1 = 0
                        dur2 = 0
                        
                        if len(parts) > 0:
                            dur1 = file_durations.get(parts[0].strip(), 0)
                        
                        if len(parts) > 1:
                            part2 = parts[1].strip()
                            if '|' in part2:
                                part2 = part2.split('|')[1]
                            dur2 = file_durations.get(part2, 0)
                            
                        max_dur = max(dur1, dur2)
                        
                        tipo = 'combinacion'
                        if strat_id == 7: tipo = 'combinacion 1-5'
                        elif strat_id == 8: tipo = 'combinacion 3-5'
                        elif strat_id == 9: tipo = 'combinacion 1-6'
                        
                        rows.append({
                            'Instruction_Time': max_dur,
                            'Tipo': tipo,
                            'actividad': act_id,
                            'HRI_strategy': strat_id,
                            'paso_actividad': paso_id
                        })
                        comb_count += 1
else:
    print(f"File not found: {json_db}")

# Ordenar por actividad, luego estrategia, luego paso
rows.sort(key=lambda x: (int(x['actividad']), int(x['HRI_strategy']), int(x['paso_actividad'])))

with open(output_csv, 'w', encoding='utf-8', newline='') as f:
    fieldnames = ['Instruction_Time', 'Tipo', 'actividad', 'HRI_strategy', 'paso_actividad']
    writer = csv.DictWriter(f, fieldnames=fieldnames)
    writer.writeheader()
    writer.writerows(rows)

print(f"\nDone. Generated {output_csv} with {len(rows)} total records.")
print(f"Audios added: {audio_count}")
print(f"Images (mp4) added: {image_count}")
print(f"3D (VideoAlpha + FBX) added: {td_count}")
print(f"Combinations added: {comb_count}")
