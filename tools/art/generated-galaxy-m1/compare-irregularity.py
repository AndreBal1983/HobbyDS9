"""Describe actual layout variation; no generation or layout/balance validation rules."""
from pathlib import Path
import json
import math
import statistics
import sys
from PIL import Image, ImageDraw, ImageFont


def percentile(values, q):
    a=sorted(values);p=(len(a)-1)*q;i=math.floor(p);j=min(i+1,len(a)-1)
    return a[i]+(a[j]-a[i])*(p-i)


def metrics(doc):
    areas=[];empty_area=0;nn=[];axis_near=0;elongations=[]
    for r in doc['Regions']:
        polygon=r['Boundary'];area=abs(sum(polygon[i]['X']*polygon[(i+1)%len(polygon)]['Y']-polygon[(i+1)%len(polygon)]['X']*polygon[i]['Y']for i in range(len(polygon))))/2
        areas.append(area)
        if not r.get('SystemId'):empty_area+=area
        xs=[p['X']for p in polygon];ys=[p['Y']for p in polygon];w=max(xs)-min(xs);h=max(ys)-min(ys);elongations.append(max(w/h,h/w))
        p=r['Center'];others=[s for s in doc['Regions']if s['NodeId']!=r['NodeId']]
        nearest=min(others,key=lambda s:((s['Center']['X']-p['X'])**2+(s['Center']['Y']-p['Y'])**2,s['NodeId']))['Center']
        dx=abs(nearest['X']-p['X']);dy=abs(nearest['Y']-p['Y']);nn.append(math.hypot(dx,dy)/doc['Width']*100)
        angle=math.degrees(math.atan2(dy,dx))
        if angle<=12 or angle>=78:axis_near+=1
    total=doc['Width']*doc['Height'];mean=statistics.mean(areas)
    return {'seed':doc['Settings']['Seed'],'systems':len(doc['Systems']),'regions':len(areas),'emptyRegions':sum(not r.get('SystemId')for r in doc['Regions']),'generatorVersion':doc['GeneratorVersion'],'tuningVersion':doc['TuningVersion'],'contentHash':doc['ContentHash'],'cellAreaCV':statistics.pstdev(areas)/mean,'cellAreaP90OverP10':percentile(areas,.9)/percentile(areas,.1),'cellAreaMinMaxPercentOfExtent':[min(areas)/total*100,max(areas)/total*100],'emptyRegionAreaPercent':empty_area/total*100,'nearestSiteSpacingP10P90PercentOfWidth':[percentile(nn,.1),percentile(nn,.9)],'nearestSiteAxisAlignedWithin12DegreesPercent':axis_near/len(areas)*100,'cellBoundingBoxElongationP90':percentile(elongations,.9),'descriptiveOnly':'These summarize visual distribution. They are not generation acceptance limits, physical distances, travel costs or approved balance.'}


def read_index(file):
    index=json.loads(file.read_text(encoding='utf-8'))
    return {(json.loads((file.parent/s['document']).read_text(encoding='utf-8'))['Settings']['Seed'],json.loads((file.parent/s['document']).read_text(encoding='utf-8'))['Settings']['SystemCount']):json.loads((file.parent/s['document']).read_text(encoding='utf-8'))for s in index['samples']}


def main():
    before_index,after_index,before_images,after_images,out=map(Path,sys.argv[1:])
    assert not out.exists(),'Preserve previous comparison; choose a new directory'
    before=read_index(before_index);after=read_index(after_index);assert before.keys()==after.keys()
    before_checks=json.loads((before_images/'review-checks.json').read_text(encoding='utf-8'));after_checks=json.loads((after_images/'review-checks.json').read_text(encoding='utf-8'));assert before_checks['sourceHashes']==after_checks['sourceHashes'],'A/B renderer sources differ'
    out.mkdir(parents=True);pairs=[]
    font=ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf',22)
    small=ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf',17)
    for seed,count in [('42',40),('43',40),('12345',30),('20261020',50)]:
        key=(seed,count);a=before[key];b=after[key];assert (a['Width'],a['Height'])==(b['Width'],b['Height'])
        assert len(a['Systems'])==len(b['Systems']) and len(a['Regions'])==len(b['Regions'])
        suffix='overview'if seed=='42'else'alternate-seed'if seed=='43'else'overview-'+str(count)
        canvas=Image.new('RGB',(2000,750),(5,10,16));draw=ImageDraw.Draw(canvas)
        draw.text((20,14),f'SAME SEED {seed} / {count} SYSTEMS / IDENTICAL FIT FRAMING',font=font,fill=(239,177,107))
        for i,(directory,title)in enumerate([(before_images,'BEFORE / irregular-regions-v1'),(after_images,'AFTER / irregular-regions-v2')]):
            image=Image.open(directory/f'standard-{suffix}.png').convert('RGB');image.thumbnail((980,552));canvas.paste(image,(10+i*1000,95));draw.text((20+i*1000,60),title,font=small,fill=(185,172,230))
        m1=metrics(a);m2=metrics(b);draw.text((20,660),f"Cell-area P90/P10: {m1['cellAreaP90OverP10']:.2f} → {m2['cellAreaP90OverP10']:.2f}   |   Area CV: {m1['cellAreaCV']:.2f} → {m2['cellAreaCV']:.2f}",font=small,fill=(238,234,242))
        draw.text((20,695),'Actual generator output only. No frontend warping or marker removal. Diagnostic values describe appearance, not balance.',font=small,fill=(169,184,200))
        file=f'compare-seed-{seed}-systems-{count}.png';canvas.save(out/file)
        pairs.append({'seed':seed,'systemCount':count,'comparisonImage':file,'before':m1,'after':m2})
    report={'status':'Same-seed actual-data visual comparison; no generator or native acceptance claim','beforeIndex':str(before_index),'afterIndex':str(after_index),'rendererSourceHashes':before_checks['sourceHashes'],'beforeBackendSource':before_checks['sourceCommit'],'afterBackendSource':after_checks['sourceCommit'],'physicalFeatureInfluence':False,'ownerAcceptance':'Geometry evidence only; environment-first causality remains a separate proposal, not implemented or accepted','framing':'Same renderer, viewport, source extent, Fit and style; only actual data changes','pairs':pairs}
    (out/'layout-diagnostics.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
    print(json.dumps([{'seed':p['seed'],'areaRatioBefore':p['before']['cellAreaP90OverP10'],'areaRatioAfter':p['after']['cellAreaP90OverP10'],'areaCVBefore':p['before']['cellAreaCV'],'areaCVAfter':p['after']['cellAreaCV']}for p in pairs]))


if __name__=='__main__':main()
