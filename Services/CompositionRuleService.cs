using PathfinderPhotography.Models;

namespace PathfinderPhotography.Services;

public class CompositionRuleService
{
    private readonly List<CompositionRule> rules =
    [
        new()
        {
            Id = 1,
            Name = "Rule of Thirds",
            Description = "Divide your frame into nine equal parts using two horizontal and two vertical lines. Place important elements along these lines or at their intersections.",
            SampleImagePath = "/samples/rule-of-thirds.jpg",
            DetailedExplanation = "The Rule of Thirds is one of the most fundamental composition techniques. Imagine your image is divided into 9 equal segments by 2 vertical and 2 horizontal lines. The theory is that if you place points of interest in the intersections or along the lines, your photo becomes more balanced and will enable a viewer of the image to interact with it more naturally."
        },

        new()
        {
            Id = 2,
            Name = "Leading Lines",
            Description = "Use natural or man-made lines to lead the viewer's eye through the photograph toward the main subject.",
            SampleImagePath = "/samples/leading-lines.jpg",
            DetailedExplanation = "Leading lines are lines within an image that lead the eye to another point in the image or occasionally, out of the image. These can be roads, fences, rivers, buildings, or any other element that creates a path for the eye to follow. They add depth and can make the viewer feel as if they are in the scene."
        },

        new()
        {
            Id = 3,
            Name = "Framing Natural",
            Description = "Use elements in the scene to create a natural frame around your subject, such as doorways, windows, or tree branches.",
            SampleImagePath = "/samples/framing-natural.jpg",
            DetailedExplanation = "Framing involves using elements within the scene to create a frame within your frame. This can isolate your main subject and draw the viewer's eye directly to it. Natural frames can be doorways, windows, arches, overhanging branches, or any other element that forms a border around your subject."
        },

        new()
        {
            Id = 4,
            Name = "Fill the Frame",
            Description =
                "Get close to your subject so that it fills the entire frame, eliminating distracting backgrounds.",
            SampleImagePath = "/samples/fill-the-frame.jpg",
            DetailedExplanation = "Filling the frame means getting close to your subject, or zooming in, so that it takes up most or all of the frame. This technique removes distracting elements and allows the viewer to focus entirely on your subject. It's particularly effective for portraits and detailed shots."
        },

        new()
        {
            Id = 5,
            Name = "Symmetry",
            Description = "Create balance through symmetrical compositions.",
            SampleImagePath = "/samples/symmetry.jpg",
            DetailedExplanation = "Symmetry in photography can create a sense of balance, harmony, and often evokes a feeling of calm. Symmetrical compositions place visual elements so that both sides of the frame mirror each other, producing a formal and restful image."
        },

        new()
        {
            Id = 6,
            Name = "Asymmetry",
            Description = "Add interest with deliberate asymmetry.",
            SampleImagePath = "/samples/asymmetry.jpg",
            DetailedExplanation = "Asymmetrical compositions are dynamic and interesting, creating tension and visual interest by placing elements off-center. They can produce energy and movement in a photograph while still maintaining visual balance through contrast, color, or negative space."
        },

        new()
        {
            Id = 7,
            Name = "Patterns & Repetition",
            Description = "Find and photograph repeating patterns, shapes, or colors that create visual rhythm.",
            SampleImagePath = "/samples/patterns-repetition.jpg",
            DetailedExplanation = "Patterns are everywhere - in nature, architecture, and everyday objects. Repetition of shapes, colors, or objects creates a visually pleasing effect. Breaking a pattern can also create a powerful focal point by drawing attention to the element that stands out."
        },

        new()
        {
            Id = 8,
            Name = "Golden Ratio",
            Description = "Use the Golden Ratio (approximately 1.618:1) to create naturally pleasing compositions based on mathematical proportions found in nature.",
            SampleImagePath = "/samples/golden-ratio.jpg",
            DetailedExplanation = "The Golden Ratio, also known as the Fibonacci Spiral, is a mathematical ratio found throughout nature. In photography, it creates compositions that feel naturally balanced and pleasing to the eye. The spiral guides the viewer's eye through the image in a natural flow."
        },

        new()
        {
            Id = 9,
            Name = "Diagonals",
            Description = "Use diagonal lines to add dynamic energy and movement to your photographs.",
            SampleImagePath = "/samples/diagonals.jpg",
            DetailedExplanation = "Diagonal lines create a sense of movement, energy, and dynamism in your photos. Unlike horizontal and vertical lines which feel stable, diagonal lines suggest action and instability in an engaging way. They can lead the eye through the frame and add depth to your images."
        },

        new()
        {
            Id = 10,
            Name = "Center Dominant Eye",
            Description = "Place the most important element or subject at the center of the frame to create impact and draw immediate attention.",
            SampleImagePath = "/samples/center-dominant-eye.jpg",
            DetailedExplanation = "While the rule of thirds suggests off-center placement, centering your subject can be very effective, especially for symmetrical subjects or when you want to create a strong, direct impact. This works particularly well for portraits, architectural details, and subjects with radial symmetry."
        },

        new()
        {
            Id = 11,
            Name = "Picture to Ground",
            Description = "Consider the relationship between the subject (figure) and the background (ground), ensuring clear separation and contrast.",
            SampleImagePath = "/samples/picture-to-ground.jpg",
            DetailedExplanation = "Figure-to-ground relationship refers to how well your subject stands out from the background. Good figure-to-ground separation ensures your subject is clearly distinguishable and doesn't get lost in the background. This can be achieved through contrasting colors, selective focus, or choosing appropriate backgrounds."
        }
    ];

    public List<CompositionRule> GetAllRules()
    {
        return this.rules;
    }

    public CompositionRule? GetRuleById(int id)
    {
        return this.rules.FirstOrDefault(r => r.Id == id);
    }
}
