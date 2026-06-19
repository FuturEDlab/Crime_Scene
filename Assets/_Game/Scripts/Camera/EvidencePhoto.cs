using System;
using System.Collections.Generic;
using UnityEngine;

// A single photo taken with the iPad camera, held in memory for the session.
public class EvidencePhoto
{
    public readonly Texture2D Image;
    public readonly DateTime TimeTaken;

    // evidenceIds of any key items that were visible in this shot.
    public readonly List<string> CapturedEvidenceIds;

    public EvidencePhoto(Texture2D image, IEnumerable<string> capturedEvidenceIds)
    {
        Image = image;
        TimeTaken = DateTime.Now;
        CapturedEvidenceIds = capturedEvidenceIds != null
            ? new List<string>(capturedEvidenceIds)
            : new List<string>();
    }

    public bool ContainsEvidence => CapturedEvidenceIds.Count > 0;
}
