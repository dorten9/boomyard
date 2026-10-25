// iPhone: picks a picture for a stencil with the system photo picker (no photo library permission needed),
// scales it down to 256 px and hands the game a PNG path (ImagePicker.OnPickedFile).
#import <UIKit/UIKit.h>
#import <PhotosUI/PhotosUI.h>

extern UIViewController* UnityGetGLViewController();
extern "C" void UnitySendMessage(const char* obj, const char* method, const char* msg);

API_AVAILABLE(ios(14))
@interface BoomPicker : NSObject <PHPickerViewControllerDelegate>
@property (nonatomic, copy) NSString* target;
@end

static NSObject* gPicker;

@implementation BoomPicker
- (void)picker:(PHPickerViewController*)picker didFinishPicking:(NSArray<PHPickerResult*>*)results
{
    [picker dismissViewControllerAnimated:YES completion:nil];
    NSString* target = self.target;
    if (results.count == 0) { UnitySendMessage(target.UTF8String, "OnPickCancelled", ""); return; }
    NSItemProvider* provider = results[0].itemProvider;
    if (![provider canLoadObjectOfClass:[UIImage class]]) { UnitySendMessage(target.UTF8String, "OnPickFailed", ""); return; }
    [provider loadObjectOfClass:[UIImage class] completionHandler:^(id<NSItemProviderReading> object, NSError* error) {
        UIImage* image = [object isKindOfClass:[UIImage class]] ? (UIImage*)object : nil;
        dispatch_async(dispatch_get_main_queue(), ^{
            if (image == nil) { UnitySendMessage(target.UTF8String, "OnPickFailed", ""); return; }
            CGSize size = image.size;
            CGFloat k = MIN(1.0, 256.0 / MAX(size.width, size.height));
            CGSize small = CGSizeMake(MAX(1.0, round(size.width * k)), MAX(1.0, round(size.height * k)));
            UIGraphicsImageRendererFormat* format = [UIGraphicsImageRendererFormat defaultFormat];
            format.scale = 1;
            UIGraphicsImageRenderer* renderer = [[UIGraphicsImageRenderer alloc] initWithSize:small format:format];
            UIImage* scaled = [renderer imageWithActions:^(UIGraphicsImageRendererContext* ctx) {
                [image drawInRect:CGRectMake(0, 0, small.width, small.height)];
            }];
            NSString* path = [NSTemporaryDirectory() stringByAppendingPathComponent:@"boomyard_pick.png"];
            if (![UIImagePNGRepresentation(scaled) writeToFile:path atomically:YES]) { UnitySendMessage(target.UTF8String, "OnPickFailed", ""); return; }
            UnitySendMessage(target.UTF8String, "OnPickedFile", path.UTF8String);
        });
    }];
}
@end

extern "C" void BoomPickImageNative(const char* target)
{
    if (@available(iOS 14, *))
    {
        PHPickerConfiguration* config = [[PHPickerConfiguration alloc] init];
        config.filter = [PHPickerFilter imagesFilter];
        config.selectionLimit = 1;
        PHPickerViewController* vc = [[PHPickerViewController alloc] initWithConfiguration:config];
        BoomPicker* picker = [[BoomPicker alloc] init];
        picker.target = [NSString stringWithUTF8String:target];
        gPicker = picker;   // the delegate is weak: keep it alive
        vc.delegate = picker;
        [UnityGetGLViewController() presentViewController:vc animated:YES completion:nil];
    }
    else UnitySendMessage(target, "OnPickFailed", "");
}
