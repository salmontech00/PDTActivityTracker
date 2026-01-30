export function initializeDragAndDrop(dotNetHelper) {
    let currentElement = null;
    let initialX = 0, initialY = 0;

    const isTouchDevice = () => {
        try {
            document.createEvent("TouchEvent");
            return true;
        } catch (e) {
            return false;
        }
    };

    // Helper to get all draggable items
    const getListItems = () => document.querySelectorAll(".task-drop-item");

    // Re-calculate visual numbers immediately
    const updateNumbering = () => {
        // We only update numbering within each category to match Blazor logic
        const categories = document.querySelectorAll(".category-drop-zone");

        categories.forEach(category => {
            const items = category.querySelectorAll(".task-drop-item");
            items.forEach((element, index) => {
                const numberSpan = element.querySelector(".task-number");
                if (numberSpan) {
                    numberSpan.textContent = `${index + 1}`;
                }
            });
        });
    };

    function dragStart(e) {
        if (e.type.startsWith('touch')) {
            initialX = e.touches[0].clientX;
            initialY = e.touches[0].clientY;
        } else {
            initialX = e.clientX;
            initialY = e.clientY;
        }

        currentElement = e.target.closest('.task-drop-item');
        if (currentElement) {
            currentElement.style.opacity = '0.5';
            if (e.dataTransfer) {
                e.dataTransfer.effectAllowed = "move";
                // Required for Firefox
                e.dataTransfer.setData("text/plain", currentElement.getAttribute("data-id"));
            }
        }
    }

    function dragOver(e) {
        e.preventDefault();
        if (e.dataTransfer) {
            e.dataTransfer.dropEffect = "move";
        }
    }

    function dragEnd(e) {
        if (currentElement) {
            currentElement.style.opacity = '1';
            currentElement = null;
        }
    }

    async function drop(e) {
        e.preventDefault();

        let newX, newY;

        if (e.type.startsWith('touch')) {
            if (e.changedTouches && e.changedTouches.length > 0) {
                newX = e.changedTouches[0].clientX;
                newY = e.changedTouches[0].clientY;
            } else {
                return; // No touch data
            }
        } else {
            newX = e.clientX;
            newY = e.clientY;
        }

        // Identify target
        let targetElement = document.elementFromPoint(newX, newY);
        if (!targetElement) return;

        // Check if we dropped on a valid item
        let closestItem = targetElement.closest('.task-drop-item');

        // Block dropping across different categories for now (maintain current behavior)
        let targetCategory = targetElement.closest('.category-drop-zone');
        let sourceCategory = currentElement.closest('.category-drop-zone');

        // 1. Category Boundary Check
        if (!targetCategory || targetCategory !== sourceCategory) {
            return;
        }

        // 2. Section Boundary Check
        // Get sections (handle empty/null sections gracefully)
        let sourceSection = currentElement.getAttribute('data-section') || "";
        let targetSection = "";

        if (closestItem) {
            targetSection = closestItem.getAttribute('data-section') || "";
        } else {
            // If dropping in empty space within category, we need to decide if allowed.
            // If the category has mixed sections, dropping in "empty space" is ambiguous.
            // Strategy: Allow only if the category has NO items or if it's the valid end of a section group.
            // For safety/simplicity in this "flat list" view:
            // If we can't find a closest item to anchor to, we might be dropping at the very end.
            // We should check the LAST item's section.
            let lastItem = targetCategory.querySelector('.task-drop-item:last-child');
            if (lastItem) {
                targetSection = lastItem.getAttribute('data-section') || "";
            }
        }

        if (sourceSection !== targetSection) {
            return; // Block move between different sections
        }

        if (!closestItem || closestItem === currentElement) return;

        // Determine position relative to target
        const rect = closestItem.getBoundingClientRect();
        const midY = rect.top + rect.height / 2;

        if (newY < midY) {
            closestItem.parentNode.insertBefore(currentElement, closestItem);
        } else {
            closestItem.parentNode.insertBefore(currentElement, closestItem.nextSibling);
        }

        finalizeDrop(targetCategory);
    }

    function finalizeDrop(categoryContainer) {
        updateNumbering();

        // Collect new order and send to Blazor
        const items = categoryContainer.querySelectorAll(".task-drop-item");
        const newOrderIds = Array.from(items).map(el => parseInt(el.getAttribute("data-id")));

        // Call C# method
        dotNetHelper.invokeMethodAsync('UpdateOrderFromJS', newOrderIds);
    }

    // Attach events to all items
    // In a real app with dynamic updates, we might need a MutationObserver or re-init
    // But for now we'll attach to document and delegate or attach to current items

    // For simplicity, we'll use event delegation on the document or specific zones if possible.
    // However, user's script attached to elements. Let's try delegation for robustness.

    const container = document.querySelector('.checklist-container'); // Root container
    if (!container) return;

    container.addEventListener("dragstart", (e) => {
        if (e.target.closest('.task-drop-item')) dragStart(e);
    });

    container.addEventListener("dragover", dragOver);
    container.addEventListener("drop", drop);
    container.addEventListener("dragend", dragEnd);

    // Touch events for mobile
    // Touch events for mobile
    container.addEventListener("touchstart", (e) => {
        if (e.target.closest('.task-drop-item')) dragStart(e);
    }, { passive: false });

    container.addEventListener("touchmove", (e) => {
        // Prevent scrolling while dragging
        if (currentElement) {
            e.preventDefault();
        }
    }, { passive: false });

    container.addEventListener("touchend", (e) => {
        if (currentElement) {
            drop(e);
            dragEnd(e); // Ensure cleanup happens on touch end
        }
    });

    // Initial numbering setup
    updateNumbering();
}
