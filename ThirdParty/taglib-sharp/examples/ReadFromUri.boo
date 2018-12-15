import TagLib from "taglib-sharp.dll"
import Gnome.Vfs from "gnome-vfs-sharp"
import System
import System.IO

def ReadFromUri(argv as (string)):
        
        if len(argv) < 1:
                print "Usage: booi ReadFromUri.boo PATH [...]"
                return
        
        Vfs.Initialize()
        
        TagLib.File.SetFileAbstractionCreator(TagLib.File.FileAbstractionCreator(CreateFile))
        
        start = DateTime.Now
        songs_read = 0
        
        try:
                for path as string in argv:
                        print path
                        
                        file_info as System.IO.FileInfo = System.IO.FileInfo(path)
                        uri as string = Gnome.Vfs.Uri.GetUriFromLocalPath (file_info.FullName)
                        file as TagLib.File = null
                        
                        try:
                                file = TagLib.File.Create(uri)
                        except UnsupportedFormatException:
                                print "UNSUPPORTED FILE: ${path}"
                                print string.Empty
                                print "---------------------------------------"
                                print string.Empty
                                continue
                        
                        print "Title:      ${file.Tag.Title}"
                        
                        if file.Tag.AlbumArtists is null:
                                print "Artists:"
                        else:
                                print "Artists:    ${string.Join ('\n            ', file.Tag.AlbumArtists)}"
                        
                        if file.Tag.Performers is null:
                                print 'Performers:'
                        else:
                                print "Performers: ${string.Join ('\n            ', file.Tag.Performers)}"
                        
                        if file.Tag.Composers is null:
                                print 'Composers:'
                        else:
                                print "Composers:  ${string.Join ('\n            ', file.Tag.Composers)}"
                        
                        print "Album:      ${file.Tag.Album}"
                        print "Comment:    ${file.Tag.Comment}"
                        
                        if file.Tag.Genres is null:
                                print 'Genres:'
                        else:
                                print "Genres:     ${string.Join ('\n            ', file.Tag.Genres)}"
                        
                        print "Year:       ${file.Tag.Year}"
                        print "Track:      ${file.Tag.Track}"
                        print "TrackCount: ${file.Tag.TrackCount}"
                        print "Disc:       ${file.Tag.Disc}"
                        print "DiscCount:  ${file.Tag.DiscCount}"
                        print "Lyrics:\n${file.Tag.Lyrics}"
                        print string.Empty
                        
                        print "Media Types: ${file.Properties.MediaTypes}"
                        print string.Empty
                        
                        for codec as ICodec in file.Properties.Codecs:
                        
                                if codec.MediaTypes & MediaTypes.Audio:
                                        print "Audio Properties : ${(codec as IAudioCodec).Description}"
                                        print "Bitrate:    ${(codec as IAudioCodec).AudioBitrate}"
                                        print "SampleRate: ${(codec as IAudioCodec).AudioSampleRate}"
                                        print "Channels:   ${(codec as IAudioCodec).AudioChannels}"
                                        print string.Empty
                                
                                if codec.MediaTypes & MediaTypes.Video:
                                        print "Video Properties : ${(codec as IVideoCodec).Description}"
                                        print "Width:      ${(codec as IVideoCodec).VideoWidth}"
                                        print "Height:     ${(codec as IVideoCodec).VideoHeight}"
                                        print string.Empty
                        
                        if file.Properties.MediaTypes:
                                print "Length:     ${file.Properties.Duration}"
                                print string.Empty
                        
                        print "Embedded Pictures: ${file.Tag.Pictures.Length}"
                        
                        for picture in file.Tag.Pictures:
                                print picture.Description
                                print "   MimeType: ${picture.MimeType}"
                                print "   Size:     ${picture.Data.Count}"
                                print "   Type:     ${picture.Type}"
                        
                        print ""
                        print "---------------------------------------"
                        print ""
                        
                        songs_read = songs_read + 1
                        
        ensure:
                Vfs.Shutdown()
        
        end as DateTime = DateTime.Now;
        
        print "Total running time:    ${end - start}"
        print "Total files read:      ${songs_read}"
        print "Average time per file: ${TimeSpan ((end - start).Ticks / songs_read)}"

class VfsFileAbstraction(TagLib.File.IFileAbstraction):
        
        _name as string
        
        def constructor(file as string):
                _name = file
        
        Name:
                get:
                        return _name
                
        ReadStream:
                get:
                        return VfsStream(_name, FileMode.Open)
                
        WriteStream:
                get:
                        return VfsStream(_name, FileMode.Open)
                
def CreateFile(path):
       return VfsFileAbstraction(path)
       
ReadFromUri(argv)
